using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester
{
    public interface IHttpClient
    {
        HttpClient Client { get; }

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
    }
}