using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public interface IRequestMapper
    {
        Task<HttpRequestMessage> Map(HttpRequest request);
    }
}