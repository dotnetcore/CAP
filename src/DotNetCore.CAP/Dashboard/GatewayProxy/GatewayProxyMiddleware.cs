using System.Collections.Generic;
using System.Net.Http;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public abstract class GatewayProxyMiddleware
    {
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        protected GatewayProxyMiddleware(IRequestScopedDataRepository requestScopedDataRepository)
        {
            _requestScopedDataRepository = requestScopedDataRepository;
            MiddlewareName = this.GetType().Name;
        }

        public string MiddlewareName { get; }

        //public DownstreamRoute DownstreamRoute => _requestScopedDataRepository.Get<DownstreamRoute>("DownstreamRoute");

        public HttpRequestMessage Request => _requestScopedDataRepository.Get<HttpRequestMessage>("Request");

        public HttpRequestMessage DownstreamRequest => _requestScopedDataRepository.Get<HttpRequestMessage>("DownstreamRequest");

        public HttpResponseMessage HttpResponseMessage => _requestScopedDataRepository.Get<HttpResponseMessage>("HttpResponseMessage");

        public void SetUpstreamRequestForThisRequest(HttpRequestMessage request)
        {
            _requestScopedDataRepository.Add("Request", request);
        }

        public void SetDownstreamRequest(HttpRequestMessage request)
        {
            _requestScopedDataRepository.Add("DownstreamRequest", request);
        }

        public void SetHttpResponseMessageThisRequest(HttpResponseMessage responseMessage)
        {
            _requestScopedDataRepository.Add("HttpResponseMessage", responseMessage);
        }
    }
}
