using System.ComponentModel.DataAnnotations;

namespace CognitoUserManager.Contracts.DTO
{
    public class UserLoginModel
    {
        [Required]
        public string EmailAddress { get; set; }

        [Required]
        public string Password { get; set; }
    }
}