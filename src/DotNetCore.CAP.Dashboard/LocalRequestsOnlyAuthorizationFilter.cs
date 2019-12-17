// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using DotNetCore.CAP.Internal;

namespace DotNetCore.CAP.Dashboard
{
    public class LocalRequestsOnlyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public Task<bool> AuthorizeAsync(DashboardContext context)
        {
            var ipAddress = context.Request.RemoteIpAddress;
            // if unknown, assume not local
            if (string.IsNullOrEmpty(ipAddress))
            {
                return Task.FromResult(false);
            }

            // check if localhost
            if (ipAddress == "127.0.0.1" || ipAddress == "0.0.0.1")
            {
                return Task.FromResult(true);
            }

            // compare with local address
            if (ipAddress == context.Request.LocalIpAddress)
            {
                return Task.FromResult(true);
            }

            // check if private ip
            if (Helper.IsInnerIP(ipAddress))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}