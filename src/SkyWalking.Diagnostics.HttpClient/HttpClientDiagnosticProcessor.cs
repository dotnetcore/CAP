/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The OpenSkywalking licenses this file to You under the Apache License, Version 2.0
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
using System.Net.Http;
using SkyWalking.Components;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;

namespace SkyWalking.Diagnostics.HttpClient
{
    public class HttpClientTracingDiagnosticProcessor : ITracingDiagnosticProcessor
    {
        public string ListenerName { get; } = "HttpHandlerDiagnosticListener";
        
        private readonly IContextCarrierFactory _contextCarrierFactory;

        public HttpClientTracingDiagnosticProcessor(IContextCarrierFactory contextCarrierFactory)
        {
            _contextCarrierFactory = contextCarrierFactory;
        }

        [DiagnosticName("System.Net.Http.Request")]
        public void HttpRequest([Property(Name = "Request")] HttpRequestMessage request)
        {
            var contextCarrier = _contextCarrierFactory.Create();
            var peer = $"{request.RequestUri.Host}:{request.RequestUri.Port}";
            var span = ContextManager.CreateExitSpan(request.RequestUri.ToString(), contextCarrier, peer);
            Tags.Url.Set(span, request.RequestUri.ToString());
            span.AsHttp();
            span.SetComponent(ComponentsDefine.HttpClient);
            Tags.HTTP.Method.Set(span, request.Method.ToString());
            foreach (var item in contextCarrier.Items)
                request.Headers.Add(item.HeadKey, item.HeadValue);
        }

        [DiagnosticName("System.Net.Http.Response")]
        public void HttpResponse([Property(Name = "Response")] HttpResponseMessage response)
        {
            var span = ContextManager.ActiveSpan;
            if (span != null && response != null)
            {
                Tags.StatusCode.Set(span, response.StatusCode.ToString());
            }

            ContextManager.StopSpan(span);
        }

        [DiagnosticName("System.Net.Http.Exception")]
        public void HttpException([Property(Name = "Request")] HttpRequestMessage request,
            [Property(Name = "Exception")] Exception ex)
        {
            var span = ContextManager.ActiveSpan;
            if (span != null && span.IsExit)
            {
                span.ErrorOccurred();
            }
        }
    }
}