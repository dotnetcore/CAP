using System.Net.Http;

namespace SkyWalking.AspNetCore
{
    public class TracingHttpClient
    {
        public HttpClient HttpClient { get; }

        public TracingHttpClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }
    }
}