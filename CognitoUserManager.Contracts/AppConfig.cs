namespace CognitoUserManager.Contracts
{
    public class AppConfig
    {
        public string Region { get; set; }
        public string UserPoolId { get; set; }
        public string AppClientId { get; set; }
        public string AccessKeyId { get; set; }
        public string AccessSecretKey { get; set; }
    }
}