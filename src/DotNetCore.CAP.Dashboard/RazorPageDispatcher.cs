// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard
{
    internal class RazorPageDispatcher : IDashboardDispatcher
    {
        private readonly Func<DashboardContext, RazorPage> _pageFunc;

        public RazorPageDispatcher(Func<DashboardContext, RazorPage> pageFunc)
        {
            _pageFunc = pageFunc;
        }

        public Task Dispatch(DashboardContext context)
        {
            context.Response.ContentType = "text/html";

            var page = _pageFunc(context);
            page.Assign(context);

            return context.Response.WriteAsync(page.ToString());
        }
    }
}