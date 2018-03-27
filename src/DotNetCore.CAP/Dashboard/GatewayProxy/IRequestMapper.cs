// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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