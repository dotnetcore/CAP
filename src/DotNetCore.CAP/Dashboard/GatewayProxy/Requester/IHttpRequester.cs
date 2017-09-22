using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester
{
    public interface IHttpRequester
    {
        Task<HttpResponseMessage> GetResponse(HttpRequestMessage request);
    }
}