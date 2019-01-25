// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DotNetCore.CAP.Dashboard.Pages;
using DotNetCore.CAP.Dashboard.Resources;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Internal;

namespace DotNetCore.CAP.Dashboard
{
    public class HtmlHelper
    {
        private readonly RazorPage _page;

        public HtmlHelper(RazorPage page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
        }

        public NonEscapedString Breadcrumbs(string title, IDictionary<string, string> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            return RenderPartial(new Breadcrumbs(title, items));
        }

        public NonEscapedString MessagesSidebar(MessageType type)
        {
            if (type == MessageType.Publish)
            {
                return SidebarMenu(MessagesSidebarMenu.PublishedItems);
            }

            return SidebarMenu(MessagesSidebarMenu.ReceivedItems);
        }

        public NonEscapedString SidebarMenu(IEnumerable<Func<RazorPage, MenuItem>> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            return RenderPartial(new SidebarMenu(items));
        }

        public NonEscapedString BlockMetric(DashboardMetric metric)
        {
            if (metric == null)
            {
                throw new ArgumentNullException(nameof(metric));
            }

            return RenderPartial(new BlockMetric(metric));
        }

        public NonEscapedString InlineMetric(DashboardMetric metric)
        {
            if (metric == null)
            {
                throw new ArgumentNullException(nameof(metric));
            }

            return RenderPartial(new InlineMetric(metric));
        }

        public NonEscapedString Paginator(Pager pager)
        {
            if (pager == null)
            {
                throw new ArgumentNullException(nameof(pager));
            }

            return RenderPartial(new Paginator(pager));
        }

        public NonEscapedString PerPageSelector(Pager pager)
        {
            if (pager == null)
            {
                throw new ArgumentNullException(nameof(pager));
            }

            return RenderPartial(new PerPageSelector(pager));
        }

        public NonEscapedString RenderPartial(RazorPage partialPage)
        {
            partialPage.Assign(_page);
            return new NonEscapedString(partialPage.ToString());
        }

        public NonEscapedString Raw(string value)
        {
            return new NonEscapedString(value);
        }

        public NonEscapedString RelativeTime(DateTime value)
        {
            return Raw($"<span data-moment=\"{Helper.ToTimestamp(value)}\">{value}</span>");
        }

        public NonEscapedString MomentTitle(DateTime time, string value)
        {
            return Raw($"<span data-moment-title=\"{Helper.ToTimestamp(time)}\">{value}</span>");
        }

        public NonEscapedString LocalTime(DateTime value)
        {
            return Raw($"<span data-moment-local=\"{Helper.ToTimestamp(value)}\">{value}</span>");
        }

        public string ToHumanDuration(TimeSpan? duration, bool displaySign = true)
        {
            if (duration == null)
            {
                return null;
            }

            var builder = new StringBuilder();
            if (displaySign)
            {
                builder.Append(duration.Value.TotalMilliseconds < 0 ? "-" : "+");
            }

            duration = duration.Value.Duration();

            if (duration.Value.Days > 0)
            {
                builder.Append($"{duration.Value.Days}d ");
            }

            if (duration.Value.Hours > 0)
            {
                builder.Append($"{duration.Value.Hours}h ");
            }

            if (duration.Value.Minutes > 0)
            {
                builder.Append($"{duration.Value.Minutes}m ");
            }

            if (duration.Value.TotalHours < 1)
            {
                if (duration.Value.Seconds > 0)
                {
                    builder.Append(duration.Value.Seconds);
                    if (duration.Value.Milliseconds > 0)
                    {
                        builder.Append($".{duration.Value.Milliseconds.ToString().PadLeft(3, '0')}");
                    }

                    builder.Append("s ");
                }
                else
                {
                    if (duration.Value.Milliseconds > 0)
                    {
                        builder.Append($"{duration.Value.Milliseconds}ms ");
                    }
                }
            }

            if (builder.Length <= 1)
            {
                builder.Append(" <1ms ");
            }

            builder.Remove(builder.Length - 1, 1);

            return builder.ToString();
        }

        public string FormatProperties(IDictionary<string, string> properties)
        {
            return string.Join(", ", properties.Select(x => $"{x.Key}: \"{x.Value}\""));
        }

        public NonEscapedString QueueLabel(string queue)
        {
            var label = queue != null
                ? $"<a class=\"text-uppercase\" href=\"{_page.Url.Queue(queue)}\">{queue}</a>"
                : $"<span class=\"label label-danger\"><i>{Strings.Common_Unknown}</i></span>";

            return new NonEscapedString(label);
        }

        public NonEscapedString ServerId(string serverId)
        {
            var parts = serverId.Split(':');
            var shortenedId = parts.Length > 1
                ? string.Join(":", parts.Take(parts.Length - 1))
                : serverId;

            return new NonEscapedString(
                $"<span class=\"labe label-defult text-uppercase\" title=\"{serverId}\">{shortenedId}</span>");
        }

        public NonEscapedString NodeSwitchLink(string id)
        {
            return Raw(
                $"<a onclick=\"nodeSwitch({id});\" href=\"javascript:;\">{Strings.NodePage_Switch}</a>");
        }

        public NonEscapedString StackTrace(string stackTrace)
        {
            try
            {
                //return new NonEscapedString(StackTraceFormatter.FormatHtml(stackTrace, StackTraceHtmlFragments));
                return new NonEscapedString(stackTrace);
            }
            catch (RegexMatchTimeoutException)
            {
                return new NonEscapedString(HtmlEncode(stackTrace));
            }
        }

        public string HtmlEncode(string text)
        {
            return WebUtility.HtmlEncode(text);
        }

        #region MethodEscaped

        public NonEscapedString MethodEscaped(MethodInfo method)
        {
            var @public = WrapKeyword("public");
            var async = string.Empty;
            string @return;

            var isAwaitable = CoercedAwaitableInfo.IsTypeAwaitable(method.ReturnType, out var coercedAwaitableInfo);
            if (isAwaitable)
            {
                async = WrapKeyword("async");
                var asyncResultType = coercedAwaitableInfo.AwaitableInfo.ResultType;

                @return = WrapType("Task") + WrapIdentifier("<") + WrapType(asyncResultType) + WrapIdentifier(">");
            }
            else
            {
                @return = WrapType(method.ReturnType);
            }

            var name = method.Name;

            string paramType = null;
            string paramName = null;

            var @params = method.GetParameters();
            if (@params.Length == 1)
            {
                var firstParam = @params[0];
                var firstParamType = firstParam.ParameterType;
                paramType = WrapType(firstParamType);
                paramName = firstParam.Name;
            }

            var paramString = paramType == null ? "();" : $"({paramType} {paramName});";

            var outputString = @public + " " + (string.IsNullOrEmpty(async) ? "" : async + " ") + @return + " " + name +
                               paramString;

            return new NonEscapedString(outputString);
        }

        private string WrapType(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            if (type.Name == "Void")
            {
                return WrapKeyword(type.Name.ToLower());
            }

            if (Helper.IsComplexType(type))
            {
                return WrapType(type.Name);
            }

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            {
                return WrapKeyword(type.Name.ToLower());
            }

            return WrapType(type.Name);
        }

        private string WrapIdentifier(string value)
        {
            return value;
        }

        private string WrapKeyword(string value)
        {
            return Span("keyword", value);
        }

        private string WrapType(string value)
        {
            return Span("type", value);
        }

        private string Span(string @class, string value)
        {
            return $"<span class=\"{@class}\">{value}</span>";
        }

        #endregion
    }
}