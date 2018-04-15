/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;

namespace SkyWalking.AspNetCore.Diagnostics
{
    public class HostingDiagnosticListener : ITracingDiagnosticListener
    {
        public HostingDiagnosticListener()
        {
        }

        public string ListenerName { get; } = "Microsoft.AspNetCore";

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn")]
        public void HttpRequestIn()
        {
            // do nothing, just enable the diagnotic source
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")]
        public void HttpRequestInStart(HttpContext httpContext)
        {
            var carrier = new ContextCarrier();
            foreach (var item in carrier.Items)
                item.HeadValue = httpContext.Request.Headers[item.HeadKey];

            var httpRequestSpan = ContextManager.CreateEntrySpan(httpContext.Request.Path, carrier);
            httpRequestSpan.AsHttp();
            httpRequestSpan.SetComponent("Asp.Net Core");
            Tags.Url.Set(httpRequestSpan, httpContext.Request.Path);
            Tags.HTTP.Method.Set(httpRequestSpan, httpContext.Request.Method);
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")]
        public void HttpRequestInStop(HttpContext httpContext)
        {
            var httpRequestSpan = ContextManager.ActiveSpan;
            var statusCode = httpContext.Response.StatusCode;
            if (statusCode >= 400)
            {
                httpRequestSpan.ErrorOccurred();
            }
            Tags.StatusCode.Set(httpRequestSpan, statusCode.ToString());
            ContextManager.StopSpan(httpRequestSpan);
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
        public void DiagnosticHandledException(HttpContext httpContext, Exception exception)
        {
            ContextManager.ActiveSpan.ErrorOccurred();
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void DiagnosticUnhandledException(HttpContext httpContext, Exception exception)
        {
            ContextManager.ActiveSpan.ErrorOccurred();
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void HostingUnhandledException(HttpContext httpContext, Exception exception)
        {
            ContextManager.ActiveSpan.ErrorOccurred();
        }
    }
}