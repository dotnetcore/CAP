using System;
using System.Collections.Concurrent;
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

    public class ScopedDataRepository : IRequestScopedDataRepository
    {
        private readonly ConcurrentDictionary<string, object> dictionary = null;

        public ScopedDataRepository()
        {
            dictionary = new ConcurrentDictionary<string, object>();
        }

        public void Add<T>(string key, T value)
        {
            dictionary.AddOrUpdate(key, value, (k, v) => value);
        }

        public T Get<T>(string key)
        {
            if (dictionary.TryGetValue(key, out object t))
            {
                return (T)t;
            }
            throw new Exception($"Unable to find data for key: {key}");
        }
    }
}