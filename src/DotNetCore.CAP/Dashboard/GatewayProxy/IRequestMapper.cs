namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public interface IRequestMapper
    {
        Task<HttpRequestMessage> Map(HttpRequest request);
    }
}