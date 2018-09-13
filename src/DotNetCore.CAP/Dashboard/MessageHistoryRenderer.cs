// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Dashboard
{
    public static class MessageHistoryRenderer
    {
        private static readonly IDictionary<string, Func<HtmlHelper, IDictionary<string, string>, NonEscapedString>>
            Renderers = new Dictionary<string, Func<HtmlHelper, IDictionary<string, string>, NonEscapedString>>();

        private static readonly IDictionary<string, string> BackgroundStateColors
            = new Dictionary<string, string>();

        private static readonly IDictionary<string, string> ForegroundStateColors
            = new Dictionary<string, string>();

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static MessageHistoryRenderer()
        {
            Register(StatusName.Succeeded, SucceededRenderer);
            Register(StatusName.Failed, FailedRenderer);

            BackgroundStateColors.Add(StatusName.Succeeded, "#EDF7ED");
            BackgroundStateColors.Add(StatusName.Failed, "#FAEBEA");
            BackgroundStateColors.Add(StatusName.Scheduled, "#E0F3F8");

            ForegroundStateColors.Add(StatusName.Succeeded, "#5cb85c");
            ForegroundStateColors.Add(StatusName.Failed, "#d9534f");
            ForegroundStateColors.Add(StatusName.Scheduled, "#5bc0de");
        }

        public static void AddBackgroundStateColor(string stateName, string color)
        {
            BackgroundStateColors.Add(stateName, color);
        }

        public static string GetBackgroundStateColor(string stateName)
        {
            if (stateName == null || !BackgroundStateColors.ContainsKey(stateName))
            {
                return "inherit";
            }

            return BackgroundStateColors[stateName];
        }

        public static void AddForegroundStateColor(string stateName, string color)
        {
            ForegroundStateColors.Add(stateName, color);
        }

        public static string GetForegroundStateColor(string stateName)
        {
            if (stateName == null || !ForegroundStateColors.ContainsKey(stateName))
            {
                return "inherit";
            }

            return ForegroundStateColors[stateName];
        }

        public static void Register(string state,
            Func<HtmlHelper, IDictionary<string, string>, NonEscapedString> renderer)
        {
            if (!Renderers.ContainsKey(state))
            {
                Renderers.Add(state, renderer);
            }
            else
            {
                Renderers[state] = renderer;
            }
        }

        public static bool Exists(string state)
        {
            return Renderers.ContainsKey(state);
        }

        public static NonEscapedString RenderHistory(
            this HtmlHelper helper,
            string state, IDictionary<string, string> properties)
        {
            var renderer = Renderers.ContainsKey(state)
                ? Renderers[state]
                : DefaultRenderer;

            return renderer?.Invoke(helper, properties);
        }

        public static NonEscapedString NullRenderer(HtmlHelper helper, IDictionary<string, string> properties)
        {
            return null;
        }

        public static NonEscapedString DefaultRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            if (stateData == null || stateData.Count == 0)
            {
                return null;
            }

            var builder = new StringBuilder();
            builder.Append("<dl class=\"dl-horizontal\">");

            foreach (var item in stateData)
            {
                builder.Append($"<dt>{item.Key}</dt>");
                builder.Append($"<dd>{item.Value}</dd>");
            }

            builder.Append("</dl>");

            return new NonEscapedString(builder.ToString());
        }

        public static NonEscapedString SucceededRenderer(HtmlHelper html, IDictionary<string, string> stateData)
        {
            var builder = new StringBuilder();
            builder.Append("<dl class=\"dl-horizontal\">");

            var itemsAdded = false;

            if (stateData.ContainsKey("Latency"))
            {
                var latency = TimeSpan.FromMilliseconds(long.Parse(stateData["Latency"]));

                builder.Append($"<dt>Latency:</dt><dd>{html.ToHumanDuration(latency, false)}</dd>");

                itemsAdded = true;
            }

            if (stateData.ContainsKey("PerformanceDuration"))
            {
                var duration = TimeSpan.FromMilliseconds(long.Parse(stateData["PerformanceDuration"]));
                builder.Append($"<dt>Duration:</dt><dd>{html.ToHumanDuration(duration, false)}</dd>");

                itemsAdded = true;
            }

            if (stateData.ContainsKey("Result") && !string.IsNullOrWhiteSpace(stateData["Result"]))
            {
                var result = stateData["Result"];
                builder.Append($"<dt>Result:</dt><dd>{WebUtility.HtmlEncode(result)}</dd>");

                itemsAdded = true;
            }

            builder.Append("</dl>");

            if (!itemsAdded)
            {
                return null;
            }

            return new NonEscapedString(builder.ToString());
        }

        private static NonEscapedString FailedRenderer(HtmlHelper html, IDictionary<string, string> stateData)
        {
            var stackTrace = html.StackTrace(stateData["ExceptionDetails"]).ToString();
            return new NonEscapedString(
                $"<h4 class=\"exception-type\">{stateData["ExceptionType"]}</h4><p class=\"text-muted\">{stateData["ExceptionMessage"]}</p>{"<pre class=\"stack-trace\">" + stackTrace + "</pre>"}");
        }

        private static NonEscapedString ProcessingRenderer(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            var builder = new StringBuilder();
            builder.Append("<dl class=\"dl-horizontal\">");

            string serverId = null;

            if (stateData.ContainsKey("ServerId"))
            {
                serverId = stateData["ServerId"];
            }
            else if (stateData.ContainsKey("ServerName"))
            {
                serverId = stateData["ServerName"];
            }

            if (serverId != null)
            {
                builder.Append("<dt>Server:</dt>");
                builder.Append($"<dd>{helper.ServerId(serverId)}</dd>");
            }

            if (stateData.ContainsKey("WorkerId"))
            {
                builder.Append("<dt>Worker:</dt>");
                builder.Append($"<dd>{stateData["WorkerId"].Substring(0, 8)}</dd>");
            }
            else if (stateData.ContainsKey("WorkerNumber"))
            {
                builder.Append("<dt>Worker:</dt>");
                builder.Append($"<dd>#{stateData["WorkerNumber"]}</dd>");
            }

            builder.Append("</dl>");

            return new NonEscapedString(builder.ToString());
        }
    }
}