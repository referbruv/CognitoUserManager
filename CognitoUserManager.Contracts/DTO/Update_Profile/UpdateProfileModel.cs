using Microsoft.AspNetCore.Http;

namespace CognitoUserManager.Contracts.DTO
{
    public class UpdateProfileModel
    {
        public string GivenName { get; set; }
        public string PhoneNumber { get; set; }
        public IFormFile ProfilePhoto { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Pincode { get; set; }
        public string UserId { get; set; }
        public string AccessToken { get; set; }
    }
}