// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard.NodeDiscovery;
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
using Microsoft.Extensions.Primitives;
// ReSharper disable UnusedMember.Global

namespace DotNetCore.CAP.Dashboard
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
        public async Task Stats()
        {
            var result = MonitoringApi.GetStatistics();
            SetServersCount(result);
            await _response.WriteAsJsonAsync(result);

            void SetServersCount(StatisticsDto dto)
            {
                if (CapCache.Global.TryGet("cap.nodes.count", out var count))
                {
                    dto.Servers = (int)count;
                }
                else
                {
                    if (ServiceProvider.GetService<DiscoveryOptions>() != null)
                    {
                        var discoveryProvider = ServiceProvider.GetRequiredService<INodeDiscoveryProvider>();
                        var nodes = discoveryProvider.GetNodes();
                        dto.Servers = nodes.Count;
                    }
                }
            }
        }

        [HttpGet("/metrics")]
        public async Task Metrics()
        {
            const string cacheKey = "dashboard.metrics";
            if (CapCache.Global.TryGet(cacheKey, out var ret))
            {
                await _response.WriteAsJsonAsync(ret);
                return;
            }

            var ps = MonitoringApi.HourlySucceededJobs(MessageType.Publish);
            var pf = MonitoringApi.HourlyFailedJobs(MessageType.Publish);
            var ss = MonitoringApi.HourlySucceededJobs(MessageType.Subscribe);
            var sf = MonitoringApi.HourlyFailedJobs(MessageType.Subscribe);

            var dayHour = ps.Keys.Select(x => x.ToString("MM-dd HH:00")).ToList(); 

            var result = new
            {
                DayHour = dayHour,
                PublishSuccessed = ps.Values,
                PublishFailed = pf.Values,
                SubscribeSuccessed = ss.Values,
                SubscribeFailed = sf.Values,
            };

            CapCache.Global.AddOrUpdate(cacheKey, result, TimeSpan.FromMinutes(10));

            await _response.WriteAsJsonAsync(result);
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
                if (message == null)
                {
                    _response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
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
                if (message == null)
                {
                    _response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
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

        [HttpPost("/received/reexecute")]
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
            var pageSize = _request.Query["perPage"].ToInt32OrDefault(20);
            var pageIndex = _request.Query["currentPage"].ToInt32OrDefault(1);
            var name = _request.Query["name"].ToString();
            var content = _request.Query["content"].ToString();
            var status = routeValue["status"]?.ToString() ?? nameof(StatusName.Succeeded);

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

            await _response.WriteAsJsonAsync(result);
        }

        [HttpGet("/received/{status}")]
        public async Task ReceivedList()
        {
            var routeValue = _routeData.Values;
            var pageSize = _request.Query["perPage"].ToInt32OrDefault(20);
            var pageIndex = _request.Query["currentPage"].ToInt32OrDefault(1);
            var name = _request.Query["name"].ToString();
            var group = _request.Query["group"].ToString();
            var content = _request.Query["content"].ToString();
            var status = routeValue["status"]?.ToString() ?? nameof(StatusName.Succeeded);

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

            await _response.WriteAsJsonAsync(result);
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

        [HttpGet("/nodes")]
        public async Task Nodes()
        {
            IList<Node> result = new List<Node>();
            var discoveryProvider = ServiceProvider.GetService<INodeDiscoveryProvider>();
            if (discoveryProvider == null)
            {
                await _response.WriteAsJsonAsync(result);
                return;
            }

            result = discoveryProvider.GetNodes();

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

    public static class IntExtension
    {
        public static int ToInt32OrDefault(this StringValues value, int defaultValue = 0)
        {
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
