using CognitoUserManager.Contracts.DTO;
using System.Threading.Tasks;

namespace CognitoUserManager.Contracts.Repositories
{
    public interface IUserRepository
    {
        Task<UserSignUpResponse> ConfirmUserSignUpAsync(UserConfirmSignUpModel model);
        Task<UserSignUpResponse> CreateUserAsync(UserSignUpModel model);
        Task<UserProfileResponse> GetUserAsync(string userId);
        Task<BaseResponseModel> TryChangePasswordAsync(ChangePwdModel model);
        Task<InitForgotPwdResponse> TryInitForgotPasswordAsync(InitForgotPwdModel model);
        Task<AuthResponseModel> TryLoginAsync(UserLoginModel model);
        Task<UserSignOutResponse> TryLogOutAsync(UserSignOutModel model);
        Task<ResetPasswordResponse> TryResetPasswordWithConfirmationCodeAsync(ResetPasswordModel model);
        Task<UpdateProfileResponse> UpdateUserAttributesAsync(UpdateProfileModel model);
    }
}