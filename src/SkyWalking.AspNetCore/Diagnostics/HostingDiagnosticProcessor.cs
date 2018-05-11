/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;
using SkyWalking.Diagnostics;
using SkyWalking.NetworkProtocol.Trace;

namespace SkyWalking.AspNetCore.Diagnostics
{
    public class HostingDiagnosticProcessor : ITracingDiagnosticProcessor
    {
        public string ListenerName { get; } = "Microsoft.AspNetCore";

        [DiagnosticName("Microsoft.AspNetCore.Hosting.BeginRequest")]
        public void BeginRequest([Property] HttpContext httpContext)
        {
            var carrier = new ContextCarrier();
            foreach (var item in carrier.Items)
                item.HeadValue = httpContext.Request.Headers[item.HeadKey];
            var httpRequestSpan = ContextManager.CreateEntrySpan($"{Config.AgentConfig.ApplicationCode} {httpContext.Request.Path}", carrier);
            httpRequestSpan.AsHttp();
            httpRequestSpan.SetComponent(ComponentsDefine.AspNetCore);
            Tags.Url.Set(httpRequestSpan, httpContext.Request.Path);
            Tags.HTTP.Method.Set(httpRequestSpan, httpContext.Request.Method);
            httpRequestSpan.Log(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                new Dictionary<string, object>
                {
                    {"event", "AspNetCore Hosting BeginRequest"},
                    {"message", $"Request starting {httpContext.Request.Protocol} {httpContext.Request.Method} {httpContext.Request.GetDisplayUrl()}"}
                });
            httpContext.Items[HttpContextDiagnosticStrings.SpanKey] = httpRequestSpan;
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.EndRequest")]
        public void EndRequest([Property] HttpContext httpContext)
        {
            var httpRequestSpan = ContextManager.ActiveSpan;
            if (httpRequestSpan == null)
            {
                return;
            }

            var statusCode = httpContext.Response.StatusCode;
            if (statusCode >= 400)
            {
                httpRequestSpan.ErrorOccurred();
            }

            Tags.StatusCode.Set(httpRequestSpan, statusCode.ToString());
            httpRequestSpan.Log(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                new Dictionary<string, object>
                {
                    {"event", "AspNetCore Hosting EndRequest"},
                    {"message", $"Request finished {httpContext.Response.StatusCode} {httpContext.Response.ContentType}"}
                });
            ContextManager.StopSpan(httpRequestSpan);
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void DiagnosticUnhandledException([Property]HttpContext httpContext, [Property]Exception exception)
        {
            ContextManager.ActiveSpan?.ErrorOccurred()?.Log(exception);
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void HostingUnhandledException([Property]HttpContext httpContext, [Property]Exception exception)
        {
            ContextManager.ActiveSpan?.ErrorOccurred()?.Log(exception);
        }
        
        [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeAction")]
        public void BeforeAction([Property]ActionDescriptor actionDescriptor, [Property]HttpContext httpContext)
        {
            var span = httpContext.Items[HttpContextDiagnosticStrings.SpanKey] as ISpan;
            if (span == null)
            {
                return;
            }

            var events = new Dictionary<string, object> {{"event", "AspNetCore.Mvc Executing action method"}, {"Action method", actionDescriptor.DisplayName}};
            span.Log(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), events);
        }

        [DiagnosticName("Microsoft.AspNetCore.Mvc.AfterAction")]
        public void AfterAction([Property]ActionDescriptor actionDescriptor, [Property]HttpContext httpContext)
        {
            var span = httpContext.Items[HttpContextDiagnosticStrings.SpanKey] as ISpan;
            if (span == null)
            {
                return;
            }

            var events = new Dictionary<string, object> {{"event", "AspNetCore.Mvc Executed action method"}};
            span.Log(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), events);
        }
    }
}