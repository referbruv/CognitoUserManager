using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using CognitoUserManager.Contracts;
using CognitoUserManager.Contracts.DTO;
using CognitoUserManager.Contracts.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CognitoUserManager.Core.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppConfig _cloudConfig;
        private readonly AmazonCognitoIdentityProviderClient _provider;
        private readonly CognitoUserPool _userPool;
        private readonly UserContextManager _userManager;
        private readonly HttpContext _httpContext;

        public UserRepository(IOptions<AppConfig> appConfig, UserContextManager userManager, IHttpContextAccessor httpContextAccessor)
        {
            _cloudConfig = appConfig.Value;
            _provider = new AmazonCognitoIdentityProviderClient(
                _cloudConfig.AccessKeyId, _cloudConfig.AccessSecretKey, RegionEndpoint.GetBySystemName(_cloudConfig.Region));
            _userPool = new CognitoUserPool(_cloudConfig.UserPoolId, _cloudConfig.AppClientId, _provider);
            _userManager = userManager;
            _httpContext = httpContextAccessor.HttpContext;
        }

        public async Task<UserSignUpResponse> CreateUserAsync(UserSignUpModel model)
        {
            //// Register the user using Cognito
            var signUpRequest = new SignUpRequest
            {
                ClientId = _cloudConfig.AppClientId,
                Password = model.Password,
                Username = model.EmailAddress
            };

            signUpRequest.UserAttributes.Add(new AttributeType
            {
                Name = "email",
                Value = model.EmailAddress
            });
            signUpRequest.UserAttributes.Add(new AttributeType
            {
                Value = model.GivenName,
                Name = "given_name"
            });
            signUpRequest.UserAttributes.Add(new AttributeType
            {
                Value = model.PhoneNumber,
                Name = "phone_number"
            });

            //if (model.ProfilePhoto != null)
            //{
            //    // upload the incoming profile photo to user's S3 folder
            //    // and get the s3 url
            //    // add the s3 url to the profile_photo attribute of the userCognito
            //    var picUrl = await _storage.AddItem(model.ProfilePhoto, "profile");

            //    signUpRequest.UserAttributes.Add(new AttributeType
            //    {
            //        Value = picUrl,
            //        Name = "picture"
            //    });
            //}

            SignUpResponse response = await _provider.SignUpAsync(signUpRequest);

            var signUpResponse = new UserSignUpResponse
            {
                UserId = response.UserSub,
                EmailAddress = model.EmailAddress,
                Message = $"Confirmation Code sent to {response.CodeDeliveryDetails.Destination} via {response.CodeDeliveryDetails.DeliveryMedium.Value}",
                Status = CognitoStatusCodes.USER_UNCONFIRMED,
                IsSuccess = true
            };

            return signUpResponse;
        }

        public async Task<UserSignUpResponse> ConfirmUserSignUpAsync(UserConfirmSignUpModel model)
        {
            ConfirmSignUpRequest request = new ConfirmSignUpRequest
            {
                ClientId = _cloudConfig.AppClientId,
                ConfirmationCode = model.ConfirmationCode,
                Username = model.EmailAddress
            };
            var response = await _provider.ConfirmSignUpAsync(request);

            // add to default users group
            //var addUserToGroupRequest = new AdminAddUserToGroupRequest
            //{
            //    UserPoolId = _cloudConfig.UserPoolId,
            //    Username = model.UserId,
            //    GroupName = "-users-group"
            //};
            //var addUserToGroupResponse = await _provider.AdminAddUserToGroupAsync(addUserToGroupRequest);

            return new UserSignUpResponse
            {
                EmailAddress = model.EmailAddress,
                UserId = model.UserId,
                Message = "User Confirmed",
                IsSuccess = true
            };
        }

        public async Task<BaseResponseModel> TryChangePasswordAsync(ChangePwdModel model)
        {
            // FetchTokens for User
            var tokenResponse = await AuthenticateUserAsync(model.EmailAddress, model.CurrentPassword);

            ChangePasswordRequest request = new ChangePasswordRequest
            {
                AccessToken = tokenResponse.Item2.AccessToken,
                PreviousPassword = model.CurrentPassword,
                ProposedPassword = model.NewPassword
            };
            ChangePasswordResponse response = await _provider.ChangePasswordAsync(request);
            return new ChangePwdResponse { UserId = tokenResponse.Item1.Username, Message = "Password Changed", IsSuccess = true };
        }

        public async Task<AuthResponseModel> TryLoginAsync(UserLoginModel model)
        {
            try
            {
                var result = await AuthenticateUserAsync(model.EmailAddress, model.Password);

                if (result.Item1.Username != null)
                {
                    await _userManager.SignIn(_httpContext, new Dictionary<string, string>() {
                        {ClaimTypes.Email, result.Item1.UserID},
                        {ClaimTypes.NameIdentifier, result.Item1.Username}
                    });
                }

                var authResponseModel = new AuthResponseModel();
                authResponseModel.EmailAddress = result.Item1.UserID;
                authResponseModel.UserId = result.Item1.Username;
                authResponseModel.Tokens = new TokenModel
                {
                    IdToken = result.Item2.IdToken,
                    AccessToken = result.Item2.AccessToken,
                    ExpiresIn = result.Item2.ExpiresIn,
                    RefreshToken = result.Item2.RefreshToken
                };
                authResponseModel.IsSuccess = true;
                return authResponseModel;
            }
            catch (UserNotConfirmedException)
            {
                var listUsersResponse = await FindUsersByEmailAddress(model.EmailAddress);

                if (listUsersResponse != null && listUsersResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    var users = listUsersResponse.Users;
                    var filtered_user = users.FirstOrDefault(x => x.Attributes.Any(x => x.Name == "email" && x.Value == model.EmailAddress));

                    var resendCodeResponse = await _provider.ResendConfirmationCodeAsync(new ResendConfirmationCodeRequest
                    {
                        ClientId = _cloudConfig.AppClientId,
                        Username = filtered_user.Username
                    });

                    if (resendCodeResponse.HttpStatusCode == HttpStatusCode.OK)
                    {
                        return new AuthResponseModel
                        {
                            IsSuccess = false,
                            Message = $"Confirmation Code sent to {resendCodeResponse.CodeDeliveryDetails.Destination} via {resendCodeResponse.CodeDeliveryDetails.DeliveryMedium.Value}",
                            Status = CognitoStatusCodes.USER_UNCONFIRMED,
                            UserId = filtered_user.Username
                        };
                    }
                    else
                    {
                        return new AuthResponseModel
                        {
                            IsSuccess = false,
                            Message = $"Resend Confirmation Code Response: {resendCodeResponse.HttpStatusCode.ToString()}",
                            Status = CognitoStatusCodes.API_ERROR,
                            UserId = filtered_user.Username
                        };
                    }
                }
                else
                {
                    return new AuthResponseModel
                    {
                        IsSuccess = false,
                        Message = "No Users found for the EmailAddress.",
                        Status = CognitoStatusCodes.USER_NOTFOUND
                    };
                }
            }
            catch (UserNotFoundException)
            {
                return new AuthResponseModel
                {
                    IsSuccess = false,
                    Message = "EmailAddress not found.",
                    Status = CognitoStatusCodes.USER_NOTFOUND
                };
            }
            catch (NotAuthorizedException)
            {
                return new AuthResponseModel
                {
                    IsSuccess = false,
                    Message = "Incorrect username or password",
                    Status = CognitoStatusCodes.API_ERROR
                };
            }
        }

        private async Task<Tuple<CognitoUser, AuthenticationResultType>> AuthenticateUserAsync(string emailAddress, string password)
        {
            CognitoUser user = new CognitoUser(emailAddress, _cloudConfig.AppClientId, _userPool, _provider);
            InitiateSrpAuthRequest authRequest = new InitiateSrpAuthRequest()
            {
                Password = password
            };

            AuthFlowResponse authResponse = await user.StartWithSrpAuthAsync(authRequest);
            var result = authResponse.AuthenticationResult;
            // return new Tuple<string, string, AuthenticationResultType>(user.UserID, user.Username, result);
            return new Tuple<CognitoUser, AuthenticationResultType>(user, result);
        }

        public async Task<UserSignOutResponse> TryLogOutAsync(UserSignOutModel model)
        {
            var request = new GlobalSignOutRequest { AccessToken = model.AccessToken };
            var response = await _provider.GlobalSignOutAsync(request);

            await _userManager.SignOut(_httpContext);
            return new UserSignOutResponse { UserId = model.UserId, Message = "User Signed Out" };
        }

        public async Task<UpdateProfileResponse> UpdateUserAttributesAsync(UpdateProfileModel model)
        {
            UpdateUserAttributesRequest userAttributesRequest = new UpdateUserAttributesRequest
            {
                AccessToken = model.AccessToken
            };

            userAttributesRequest.UserAttributes.Add(new AttributeType
            {
                Value = model.GivenName,
                Name = "given_name"
            });

            userAttributesRequest.UserAttributes.Add(new AttributeType
            {
                Value = model.PhoneNumber,
                Name = "phone_number"
            });

            // upload the incoming profile photo to user's S3 folder
            // and get the s3 url
            // add the s3 url to the profile_photo attribute of the userCognito
            // if (model.ProfilePhoto != null)
            // {
            //     var picUrl = await _storage.AddItem(model.ProfilePhoto, "profile");
            //     userAttributesRequest.UserAttributes.Add(new AttributeType
            //     {
            //         Value = picUrl,
            //         Name = "picture"
            //     });
            // }

            if (model.Gender != null)
            {
                userAttributesRequest.UserAttributes.Add(new AttributeType
                {
                    Value = model.Gender,
                    Name = "gender"
                });
            }

            if (!string.IsNullOrEmpty(model.Address) ||
                string.IsNullOrEmpty(model.State) ||
                string.IsNullOrEmpty(model.Country) ||
                string.IsNullOrEmpty(model.Pincode))
            {
                var dictionary = new Dictionary<string, string>();

                dictionary.Add("street_address", model.Address);
                dictionary.Add("region", model.State);
                dictionary.Add("country", model.Country);
                dictionary.Add("postal_code", model.Pincode);

                userAttributesRequest.UserAttributes.Add(new AttributeType
                {
                    Value = JsonConvert.SerializeObject(dictionary),
                    Name = "address"
                });
            }

            var response = await _provider.UpdateUserAttributesAsync(userAttributesRequest);
            return new UpdateProfileResponse { UserId = model.UserId, Message = "Profile Updated", IsSuccess = true };
        }

        public async Task<InitForgotPwdResponse> TryInitForgotPasswordAsync(InitForgotPwdModel model)
        {
            var listUsersResponse = await FindUsersByEmailAddress(model.EmailAddress);

            if (listUsersResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                var users = listUsersResponse.Users;
                var filtered_user = users.FirstOrDefault(x => x.Attributes.Any(x => x.Name == "email" && x.Value == model.EmailAddress));
                if (filtered_user != null)
                {
                    var forgotPasswordResponse = await _provider.ForgotPasswordAsync(new ForgotPasswordRequest
                    {
                        ClientId = _cloudConfig.AppClientId,
                        Username = filtered_user.Username
                    });

                    if (forgotPasswordResponse.HttpStatusCode == HttpStatusCode.OK)
                    {
                        return new InitForgotPwdResponse
                        {
                            IsSuccess = true,
                            Message = $"Confirmation Code sent to {forgotPasswordResponse.CodeDeliveryDetails.Destination} via {forgotPasswordResponse.CodeDeliveryDetails.DeliveryMedium.Value}",
                            UserId = filtered_user.Username,
                            EmailAddress = model.EmailAddress,
                            Status = CognitoStatusCodes.USER_UNCONFIRMED
                        };
                    }
                    else
                    {
                        return new InitForgotPwdResponse
                        {
                            IsSuccess = false,
                            Message = $"ListUsers Response: {forgotPasswordResponse.HttpStatusCode.ToString()}",
                            Status = CognitoStatusCodes.API_ERROR
                        };
                    }
                }
                else
                {
                    return new InitForgotPwdResponse
                    {
                        IsSuccess = false,
                        Message = $"No users with the given emailAddress found.",
                        Status = CognitoStatusCodes.USER_NOTFOUND
                    };
                }
            }
            else
            {
                return new InitForgotPwdResponse
                {
                    IsSuccess = false,
                    Message = $"ListUsers Response: {listUsersResponse.HttpStatusCode.ToString()}",
                    Status = CognitoStatusCodes.API_ERROR
                };
            }
        }

        public async Task<ResetPasswordResponse> TryResetPasswordWithConfirmationCodeAsync(ResetPasswordModel model)
        {
            var response = await _provider.ConfirmForgotPasswordAsync(new ConfirmForgotPasswordRequest
            {
                ClientId = _cloudConfig.AppClientId,
                Username = model.UserId,
                Password = model.NewPassword,
                ConfirmationCode = model.ConfirmationCode
            });

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return new ResetPasswordResponse
                {
                    IsSuccess = true,
                    Message = "Password Updated. Please Login."
                };
            }
            else
            {
                return new ResetPasswordResponse
                {
                    IsSuccess = false,
                    Message = $"ResetPassword Response: {response.HttpStatusCode.ToString()}",
                    Status = CognitoStatusCodes.API_ERROR
                };
            }
        }

        private async Task<ListUsersResponse> FindUsersByEmailAddress(string emailAddress)
        {
            ListUsersRequest listUsersRequest = new ListUsersRequest
            {
                UserPoolId = _cloudConfig.UserPoolId,
                Filter = $"email=\"{emailAddress}\""
            };
            return await _provider.ListUsersAsync(listUsersRequest);
        }

        public async Task<UserProfileResponse> GetUserAsync(string userId)
        {
            var userResponse = await _provider.AdminGetUserAsync(new AdminGetUserRequest
            {
                Username = userId,
                UserPoolId = _cloudConfig.UserPoolId
            });

            // var user = _userPool.GetUser(userId);

            var attributes = userResponse.UserAttributes;
            var response = new UserProfileResponse
            {
                EmailAddress = attributes.GetValueOrDefault("email", string.Empty),
                GivenName = attributes.GetValueOrDefault("given_name", string.Empty),
                PhoneNumber = attributes.GetValueOrDefault("phone_number", string.Empty),
                Gender = attributes.GetValueOrDefault("gender", string.Empty),
                UserId = userId
            };

            var address = attributes.GetValueOrDefault("address", string.Empty);
            if (!string.IsNullOrEmpty(address))
            {
                response.Address = JsonConvert.DeserializeObject<Dictionary<string, string>>(address);
            }

            return response;
        }
    }

    internal static class AttributeTypeExtension
    {
        public static string GetValueOrDefault(this List<AttributeType> attributeTypes, string propertyName, string defaultValue)
        {
            var prop = attributeTypes.FirstOrDefault(x => x.Name == propertyName);
            if (prop != null) return prop.Value;
            else return defaultValue;
        }
    }
}
