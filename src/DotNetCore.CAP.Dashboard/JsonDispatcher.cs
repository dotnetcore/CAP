// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks; 

namespace DotNetCore.CAP.Dashboard
{
    internal class JsonDispatcher : IDashboardDispatcher
    {
        private readonly Func<DashboardContext, object> _command;
        private readonly Func<DashboardContext, string> _jsonCommand;

        public JsonDispatcher(Func<DashboardContext, object> command)
        {
            _command = command;
        }

        public JsonDispatcher(Func<DashboardContext, string> command)
        {
            _jsonCommand = command;
        }

        public async Task Dispatch(DashboardContext context)
        {
            string serialized = null;
            if (_command != null)
            {
                var result = _command(context);
                 
                serialized = JsonSerializer.Serialize(result);
            }

            if (_jsonCommand != null)
            {
                serialized = _jsonCommand(context);
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(serialized ?? string.Empty);
        }
    }
}