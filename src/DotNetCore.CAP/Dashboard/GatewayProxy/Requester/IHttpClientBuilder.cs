using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester
{
    public interface IHttpClientBuilder
    {
        /// <summary>
        /// Creates the <see cref="HttpClient"/>
        /// </summary>
        IHttpClient Create();
    }
}
