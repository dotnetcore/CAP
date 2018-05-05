// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Dashboard
{
    public interface IDashboardAuthorizationFilter
    {
        bool Authorize(DashboardContext context);
    }
}