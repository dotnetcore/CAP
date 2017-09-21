using System.Collections.Generic;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public interface IRequestScopedDataRepository
    {
       void  Add<T>(string key, T value);
        T Get<T>(string key);
    }
}