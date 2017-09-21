using System;
using Microsoft.AspNetCore.Http;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public class HttpDataRepository : IRequestScopedDataRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpDataRepository(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Add<T>(string key, T value)
        {
            _httpContextAccessor.HttpContext.Items.Add(key, value);
        }

        public T Get<T>(string key)
        {
            object obj;

            if (_httpContextAccessor.HttpContext.Items.TryGetValue(key, out obj))
            {
                return (T)obj;
            }
            throw new Exception($"Unable to find data for key: {key}");
        }
    }
}
