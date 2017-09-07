using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace DotNetCore.CAP.Dashboard
{
    internal class JsonDispatcher : IDashboardDispatcher
    {
        private readonly Func<DashboardContext, object> _command;

        public JsonDispatcher(Func<DashboardContext, object> command)
        {
            _command = command;
        }

        public async Task Dispatch(DashboardContext context)
        {
            var request = context.Request;
            var response = context.Response;

            object result = _command(context);
            
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new JsonConverter[] { new StringEnumConverter { CamelCaseText = true } }
            };
            var serialized = JsonConvert.SerializeObject(result, settings);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(serialized);
        }
    }
}
