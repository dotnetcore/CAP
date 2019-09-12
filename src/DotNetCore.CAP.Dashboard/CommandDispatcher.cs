// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard
{
    internal class CommandDispatcher : IDashboardDispatcher
    {
        private readonly Func<DashboardContext, bool> _command;

        public CommandDispatcher(Func<DashboardContext, bool> command)
        {
            _command = command;
        }

        public Task Dispatch(DashboardContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (!"POST".Equals(request.Method, StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                return Task.FromResult(false);
            }

            if (_command(context))
            {
                response.StatusCode = (int) HttpStatusCode.NoContent;
            }
            else
            {
                response.StatusCode = 422;
            }

            return Task.FromResult(true);
        }
    }
}