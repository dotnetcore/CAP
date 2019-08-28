// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Dashboard
{
    public class LocalRequestsOnlyAuthorizationFilter : IDashboardAuthorizationFilter
    {
#pragma warning disable 1998
        public async Task<bool> AuthorizeAsync(DashboardContext context)
#pragma warning restore 1998
        {
            var ipAddress = context.Request.RemoteIpAddress;
            // if unknown, assume not local
            if (string.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            // check if localhost
            if (ipAddress == "127.0.0.1" || ipAddress == "0.0.0.1")
            {
                return true;
            }

            // compare with local address
            if (ipAddress == context.Request.LocalIpAddress)
            {
                return true;
            }

            // check if private ip
            if (Helper.IsInnerIP(ipAddress))
            {
                return true;
            }

            return false;
        }
    }
}