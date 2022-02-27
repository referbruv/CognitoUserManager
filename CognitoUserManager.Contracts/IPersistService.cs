namespace CognitoUserManager.Contracts.Services
{
    public interface IPersistService
    {
        T Get<T>(string key);
        void Set<T>(string key, T value);
        void Remove(string key);
    }
}
