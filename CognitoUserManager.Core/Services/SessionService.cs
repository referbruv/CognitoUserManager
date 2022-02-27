using CognitoUserManager.Contracts.Services;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace CognitoUserManager.Core
{
    public class SessionService : IPersistService
    {
        private readonly HttpContext _httpContext;

        public SessionService(IHttpContextAccessor contextAccessor)
        {
            _httpContext = contextAccessor.HttpContext;
        }

        public void Set<T>(string key, T value)
        {
            _httpContext.Session.SetString(key, JsonSerializer.Serialize(value));
        }

        public T Get<T>(string key)
        {
            var value = _httpContext.Session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }

        public void Remove(string key)
        {
            _httpContext.Session.Remove(key);
        }
    }
}
