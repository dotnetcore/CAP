using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using DotNetCore.CAP.Transport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.UI
{
    public class RouteActionProvider
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;
        private readonly RouteData _routeData;

        private IServiceProvider ServiceProvider => _request.HttpContext.RequestServices;
        private IMonitoringApi MonitoringApi => ServiceProvider.GetRequiredService<IDataStorage>().GetMonitoringApi();

        public RouteActionProvider(HttpRequest request, HttpResponse response, RouteData routeData)
        {
            _request = request;
            _response = response;
            _routeData = routeData;
            _response.StatusCode = StatusCodes.Status200OK;
        }

        [HttpGet("/stats")]
        public Task Stats()
        {
            //TODO
            return Task.CompletedTask;
        }

        [HttpGet("/health")]
        public Task Health()
        {
            _response.WriteAsync("OK");
            return Task.CompletedTask;
        }

        [HttpGet("/published/message/{id:long}")]
        public async Task PublishedMessageDetails()
        {
            if (long.TryParse(_routeData.Values["id"]?.ToString() ?? string.Empty, out long id))
            {
                var message = await MonitoringApi.GetPublishedMessageAsync(id);

                await _response.WriteAsJsonAsync(message.Content);
            }
            else
            {
                BadRequest();
            }
        }

        [HttpGet("/received/message/{id:long}")]
        public async Task ReceivedMessageDetails()
        {
            if (long.TryParse(_routeData.Values["id"]?.ToString() ?? string.Empty, out long id))
            {
                var message = await MonitoringApi.GetReceivedMessageAsync(id);

                await _response.WriteAsJsonAsync(message.Content);
            }
            else
            {
                BadRequest();
            }
        }

        [HttpPost("/published/requeue")]
        public async Task PublishedRequeue()
        {
            //var form = await _request.ReadFormAsync();
            //var messageIds =  form["messages[]"]
            var messageIds = await _request.ReadFromJsonAsync<long[]>();
            if (messageIds == null || messageIds.Length == 0)
            {
                _response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                return;
            }

            foreach (var messageId in messageIds)
            {
                var message = await MonitoringApi.GetPublishedMessageAsync(messageId);
                message.Origin = ServiceProvider.GetRequiredService<ISerializer>().Deserialize(message.Content);
                ServiceProvider.GetRequiredService<IDispatcher>().EnqueueToPublish(message);
            }

            _response.StatusCode = StatusCodes.Status204NoContent;
        }

        [HttpPost("/received/requeue")]
        public async Task ReceivedRequeue()
        {
            //var form = await _request.ReadFormAsync();
            //var messageIds =  form["messages[]"]
            var messageIds = await _request.ReadFromJsonAsync<long[]>();
            if (messageIds == null || messageIds.Length == 0)
            {
                _response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                return;
            }

            foreach (var messageId in messageIds)
            {
                var message = await MonitoringApi.GetReceivedMessageAsync(messageId);
                message.Origin = ServiceProvider.GetRequiredService<ISerializer>().Deserialize(message.Content);
                await ServiceProvider.GetRequiredService<ISubscribeDispatcher>().DispatchAsync(message);
            }

            _response.StatusCode = StatusCodes.Status204NoContent;
        }

        [HttpGet("/published/{status}")]
        public async Task PublishedList()
        {
            var routeValue = _routeData.Values;
            var pageSize = int.Parse(_request.Query["count"]);
            var pageIndex = int.Parse(_request.Query["from"]);
            var name = _request.Query["name"].ToString();
            var content = _request.Query["content"].ToString();
            var status = routeValue["status"]?.ToString();

            var queryDto = new MessageQueryDto
            {
                MessageType = MessageType.Publish,
                Name = name,
                Content = content,
                StatusName = status,
                CurrentPage = pageIndex - 1,
                PageSize = pageSize
            };

            var result = MonitoringApi.Messages(queryDto);

            var count = string.Equals(status, nameof(StatusName.Succeeded), StringComparison.CurrentCultureIgnoreCase)
                ? MonitoringApi.PublishedSucceededCount()
                : MonitoringApi.PublishedFailedCount();

            await _response.WriteAsJsonAsync(new { count, result });
        }

        [HttpGet("/received/{status}")]
        public async Task ReceivedList()
        {
            var routeValue = _routeData.Values;
            var pageSize = int.Parse(_request.Query["count"]);
            var pageIndex = int.Parse(_request.Query["from"]);
            var name = _request.Query["name"].ToString();
            var group = _request.Query["group"].ToString();
            var content = _request.Query["content"].ToString();
            var status = routeValue["status"]?.ToString();

            var queryDto = new MessageQueryDto
            {
                MessageType = MessageType.Subscribe,
                Group = group,
                Name = name,
                Content = content,
                StatusName = status,
                CurrentPage = pageIndex - 1,
                PageSize = pageSize
            };

            var result = MonitoringApi.Messages(queryDto);

            var count = string.Equals(status, nameof(StatusName.Succeeded), StringComparison.CurrentCultureIgnoreCase)
                ? MonitoringApi.ReceivedSucceededCount()
                : MonitoringApi.ReceivedFailedCount();

            await _response.WriteAsJsonAsync(new { count, result });
        }

        [HttpGet("/subscriber")]
        public async Task Subscribers()
        {
            var cache = ServiceProvider.GetRequiredService<MethodMatcherCache>();
            var subscribers = cache.GetCandidatesMethodsOfGroupNameGrouped();

            var result = new List<WarpResult>();

            foreach (var subscriber in subscribers)
            {
                var inner = new WarpResult()
                {
                    Group = subscriber.Key,
                    Values = new List<WarpResult.SubInfo>()
                };
                foreach (var descriptor in subscriber.Value)
                {
                    inner.Values.Add(new WarpResult.SubInfo()
                    {
                        Topic = descriptor.TopicName,
                        ImplName = descriptor.ImplTypeInfo.Name,
                        MethodEscaped = HtmlHelper.MethodEscaped(descriptor.MethodInfo)
                    });
                }
                result.Add(inner);
            }

            await _response.WriteAsJsonAsync(result);
        }

        private void BadRequest()
        {
            _response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    public class WarpResult
    {
        public int ChildCount => Values.Count;

        public string Group { get; set; }

        public List<SubInfo> Values { get; set; }

        public class SubInfo
        {
            public string Topic { get; set; }

            public string ImplName { get; set; }

            public string MethodEscaped { get; set; }
        }
    }
}
