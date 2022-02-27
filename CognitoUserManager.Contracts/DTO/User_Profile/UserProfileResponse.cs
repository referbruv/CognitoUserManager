using System.Collections.Generic;

namespace CognitoUserManager.Contracts.DTO
{
    public class UserProfileResponse : BaseResponseModel
    {
        public UserProfileResponse()
        {
            Address = new Dictionary<string, string>();
        }

        public string EmailAddress { get; set; }
        public string GivenName { get; set; }
        public string PhoneNumber { get; set; }
        public string UserId { get; set; }
        public Dictionary<string, string> Address { get; set; }
        public string Gender { get; set; }
    }
}