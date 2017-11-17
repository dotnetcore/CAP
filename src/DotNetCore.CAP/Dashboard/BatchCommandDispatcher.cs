using System;
using System.Net;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard
{
    internal class BatchCommandDispatcher : IDashboardDispatcher
    {
        private readonly Action<DashboardContext, int> _command;

        public BatchCommandDispatcher(Action<DashboardContext, int> command)
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
                var id = int.Parse(messageId);
                _command(context, id);
            }

            context.Response.StatusCode = (int) HttpStatusCode.NoContent;
        }
    }
}