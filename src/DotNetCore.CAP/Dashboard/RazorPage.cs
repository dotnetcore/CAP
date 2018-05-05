// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.NodeDiscovery;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Dashboard
{
    public abstract class RazorPage
    {
        private readonly StringBuilder _content = new StringBuilder();
        private string _body;
        private Lazy<StatisticsDto> _statisticsLazy;

        protected RazorPage()
        {
            GenerationTime = Stopwatch.StartNew();
            Html = new HtmlHelper(this);
        }

        protected RazorPage Layout { get; set; }
        protected HtmlHelper Html { get; }
        public UrlHelper Url { get; private set; }

        protected IStorage Storage { get; set; }
        protected string AppPath { get; set; }
        protected string NodeName { get; set; }

        protected int StatsPollingInterval { get; set; }
        protected Stopwatch GenerationTime { get; private set; }

        public StatisticsDto Statistics
        {
            get
            {
                if (_statisticsLazy == null)
                {
                    throw new InvalidOperationException("Page is not initialized.");
                }

                return _statisticsLazy.Value;
            }
        }

        private DashboardRequest Request { get; set; }
        private DashboardResponse Response { get; set; }
        internal IServiceProvider RequestServices { get; private set; }

        public string RequestPath => Request.Path;

        /// <exclude />
        protected abstract void Execute();

        protected string Query(string key)
        {
            return Request.GetQuery(key);
        }

        public override string ToString()
        {
            return TransformText(null);
        }

        /// <exclude />
        public void Assign(RazorPage parentPage)
        {
            Request = parentPage.Request;
            Response = parentPage.Response;
            Storage = parentPage.Storage;
            AppPath = parentPage.AppPath;
            NodeName = parentPage.NodeName;
            StatsPollingInterval = parentPage.StatsPollingInterval;
            Url = parentPage.Url;
            RequestServices = parentPage.RequestServices;
            GenerationTime = parentPage.GenerationTime;
            _statisticsLazy = parentPage._statisticsLazy;
        }

        internal void Assign(DashboardContext context)
        {
            Request = context.Request;
            Response = context.Response;
            RequestServices = context.RequestServices;
            Storage = context.Storage;
            AppPath = context.Options.AppPath;
            NodeName = GetNodeName();
            StatsPollingInterval = context.Options.StatsPollingInterval;
            Url = new UrlHelper(context);

            _statisticsLazy = new Lazy<StatisticsDto>(() =>
            {
                var monitoring = Storage.GetMonitoringApi();
                var dto = monitoring.GetStatistics();

                SetServersCount(dto);

                return dto;
            });
        }

        private string GetNodeName()
        {
            var discoveryOptions = RequestServices.GetService<DiscoveryOptions>();
            if (discoveryOptions != null)
            {
                return $"{discoveryOptions.NodeName}({discoveryOptions.NodeId})";
            }

            return null;
        }

        private void SetServersCount(StatisticsDto dto)
        {
            if (CapCache.Global.TryGet("cap.nodes.count", out var count))
            {
                dto.Servers = (int) count;
            }
            else
            {
                if (RequestServices.GetService<DiscoveryOptions>() != null)
                {
                    var discoveryProvider = RequestServices.GetService<INodeDiscoveryProvider>();
                    var nodes = discoveryProvider.GetNodes().GetAwaiter().GetResult();
                    dto.Servers = nodes.Count;
                }
            }
        }

        /// <exclude />
        protected void WriteLiteral(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }

            _content.Append(textToAppend);
        }

        /// <exclude />
        protected virtual void Write(object value)
        {
            if (value == null)
            {
                return;
            }

            var html = value as NonEscapedString;
            WriteLiteral(html?.ToString() ?? Encode(value.ToString()));
        }

        protected virtual object RenderBody()
        {
            return new NonEscapedString(_body);
        }

        private string TransformText(string body)
        {
            _body = body;

            Execute();

            if (Layout != null)
            {
                Layout.Assign(this);
                return Layout.TransformText(_content.ToString());
            }

            return _content.ToString();
        }

        private static string Encode(string text)
        {
            return string.IsNullOrEmpty(text)
                ? string.Empty
                : WebUtility.HtmlEncode(text);
        }
    }
}