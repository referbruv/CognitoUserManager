using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CognitoUserManager.Core.Repositories;
using CognitoUserManager.Contracts.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CognitoUserManager.Contracts.Services;
using CognitoUserManager.Contracts.Repositories;

namespace CognitoUserManager.Controllers
{
    public class UserController : Controller
    {
        public const string Session_TokenKey = "_Tokens";
        private readonly IUserRepository _userService;
        private readonly IPersistService _cache;

        public UserController(IUserRepository userService, IPersistService cache)
        {
            _userService = userService;
            _cache = cache;
        }

        #region Landing-TokensPage

        [Authorize]
        public async Task<IActionResult> IndexAsync()
        {
            var id = User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).First();
            var response = await _userService.GetUserAsync(id.Value);

            var model = new UpdateProfileModel
            {
                UserId = id.Value,
                GivenName = response.GivenName,
                PhoneNumber = response.PhoneNumber,
                Pincode = response.Address.GetOrDefaultValue("postal_code"),
                Country = response.Address.GetOrDefaultValue("country"),
                State = response.Address.GetOrDefaultValue("region"),
                Address = response.Address.GetOrDefaultValue("street_address"),
                Gender = response.Gender
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> IndexAsync(UpdateProfileModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var userId = User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).First();

            var token = _cache.Get<TokenModel>($"{userId.Value}_{Session_TokenKey}");

            model.AccessToken = token.AccessToken;

            var response = await _userService.UpdateUserAttributesAsync(model);

            if (response.IsSuccess)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        #endregion

        #region ExistingUser-Login

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsync(UserLoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var response = await _userService.TryLoginAsync(model);

            if (response.IsSuccess)
            {
                _cache.Set<TokenModel>($"{response.UserId}_{Session_TokenKey}", response.Tokens);
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        #endregion

        #region NewUser-Signup

        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignupAsync(UserSignUpModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var response = await _userService.CreateUserAsync(model);

            if (response.IsSuccess)
            {
                TempData["UserId"] = response.UserId;
                TempData["EmailAddress"] = response.EmailAddress;
            }

            return RedirectToAction("ConfirmSignup");
        }

        public IActionResult ConfirmSignup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmSignupAsync(UserConfirmSignUpModel model)
        {
            var response = await _userService.ConfirmUserSignUpAsync(model);

            if (response.IsSuccess)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        #endregion

        #region Change-Password

        [Authorize]
        public IActionResult ChangePassword()
        {
            var email = User.Claims.Where(x => x.Type == ClaimTypes.Email).First();
            var model = new ChangePwdModel
            {
                EmailAddress = email.Value
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePwdModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _userService.TryChangePasswordAsync(model);

            if (response.IsSuccess)
            {
                return RedirectToAction("Logout");
            }

            return View(model);
        }

        #endregion

        [Authorize]
        public async Task<IActionResult> LogOutAsync()
        {
            var userId = User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).First();

            var tokens = _cache.Get<TokenModel>($"{userId.Value}_{Session_TokenKey}");

            var user = new UserSignOutModel
            {
                AccessToken = tokens.AccessToken,
                UserId = userId.Value
            };

            _cache.Remove($"{userId.Value}_{Session_TokenKey}");

            await _userService.TryLogOutAsync(user);

            return RedirectToAction("Index");
        }

        #region Forgot-Password

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPasswordAsync(InitForgotPwdModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _userService.TryInitForgotPasswordAsync(model);

            if (response.IsSuccess)
            {
                TempData["EmailAddress"] = response.EmailAddress;
                TempData["UserId"] = response.UserId;

                return RedirectToAction("ResetPasswordWithConfirmationCode");
            }

            return View();
        }

        public IActionResult ResetPasswordWithConfirmationCode()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPasswordWithConfirmationCodeAsync(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _userService.TryResetPasswordWithConfirmationCodeAsync(model);

            if (response.IsSuccess)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        #endregion
    }

    public static class SessionExtensions
    {
        public static K GetOrDefaultValue<T, K>(this Dictionary<T, K> dictionary, T key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : default;
        }
    }
}