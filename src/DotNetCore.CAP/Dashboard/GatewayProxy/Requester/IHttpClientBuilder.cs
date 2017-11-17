using System.Net.Http;

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester
{
    public interface IHttpClientBuilder
    {
        /// <summary>
        /// Creates the <see cref="HttpClient" />
        /// </summary>
        IHttpClient Create();
    }
}