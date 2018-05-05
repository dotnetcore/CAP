// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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