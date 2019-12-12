// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard
{
    internal class BatchCommandDispatcher : IDashboardDispatcher
    {
        private readonly Action<DashboardContext, long> _command;

        public BatchCommandDispatcher(Action<DashboardContext, long> command)
        {
            _command = command;
        }

        public async Task Dispatch(DashboardContext context)
        {
            var messageIds = await context.Request.GetFormValuesAsync("messages[]");
            if (messageIds.Count == 0)
            {
                context.Response.StatusCode = 422;
                return;
            }

            foreach (var messageId in messageIds)
            {
                var id = long.Parse(messageId);
                _command(context, id);
            }

            context.Response.StatusCode = (int) HttpStatusCode.NoContent;
        }
    }
}