namespace CognitoUserManager.Contracts.DTO
{
    public class UserSignUpResponse : BaseResponseModel
    {
        public string UserId { get; set; }
        public string EmailAddress { get; set; }
    }
}