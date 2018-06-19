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
using System.Web;
using SkyWalking.Boot;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;
using SkyWalking.NetworkProtocol.Trace;
using SkyWalking.Remote;
using SkyWalking.Utils;

namespace SkyWalking.AspNet
{
    public class SkyWalkingModule : IHttpModule
    {
        private readonly SkyWalkingStartup _skyWalkingStartup = new SkyWalkingStartup();
        
        public void Dispose()
        {
            _skyWalkingStartup.Dispose();
        }

        public void Init(HttpApplication application)
        {
            _skyWalkingStartup.Start();
            application.BeginRequest += ApplicationOnBeginRequest;
            application.EndRequest += ApplicationOnEndRequest;
        }

        private void ApplicationOnBeginRequest(object sender, EventArgs e)
        {
            var httpApplication = sender as HttpApplication;
            var httpContext = httpApplication.Context;
            var carrier = new ContextCarrier();
            foreach (var item in carrier.Items)
                item.HeadValue = httpContext.Request.Headers[item.HeadKey];
            var httpRequestSpan = ContextManager.CreateEntrySpan($"{Config.AgentConfig.ApplicationCode} {httpContext.Request.Path}", carrier);
            httpRequestSpan.AsHttp();
            httpRequestSpan.SetComponent(ComponentsDefine.AspNet);
            Tags.Url.Set(httpRequestSpan, httpContext.Request.Path);
            Tags.HTTP.Method.Set(httpRequestSpan, httpContext.Request.HttpMethod);
            httpRequestSpan.Log(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                new Dictionary<string, object>
                {
                    {"event", "AspNet BeginRequest"},
                    {"message", $"Request starting {httpContext.Request.Url.Scheme} {httpContext.Request.HttpMethod} {httpContext.Request.Url.OriginalString}"}
                });
        }
        
        private void ApplicationOnEndRequest(object sender, EventArgs e)
        {
            var httpRequestSpan = ContextManager.ActiveSpan;
            if (httpRequestSpan == null)
            {
                return;
            }

            var httpApplication = sender as HttpApplication;
            var httpContext = httpApplication.Context;
            
            var statusCode = httpContext.Response.StatusCode;
            if (statusCode >= 400)
            {
                httpRequestSpan.ErrorOccurred();
            }

            Tags.StatusCode.Set(httpRequestSpan, statusCode.ToString());

            var exception = httpContext.Error;
            if (exception != null)
            {
                httpRequestSpan.ErrorOccurred().Log(exception);
            }
            
            httpRequestSpan.Log(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                new Dictionary<string, object>
                {
                    {"event", "AspNetCore Hosting EndRequest"},
                    {"message", $"Request finished {httpContext.Response.StatusCode} {httpContext.Response.ContentType}"}
                });
            ContextManager.StopSpan(httpRequestSpan);
        }
    }
}
