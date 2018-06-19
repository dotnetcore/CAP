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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;
using SkyWalking.NetworkProtocol.Trace;

namespace SkyWalking.AspNet
{
    public class HttpTracingHandler : DelegatingHandler
    {
        public HttpTracingHandler()
            : this(new HttpClientHandler())
        {
        }

        public HttpTracingHandler(HttpMessageHandler innerHandler)
        {
            InnerHandler = innerHandler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var peer = $"{request.RequestUri.Host}:{request.RequestUri.Port}";
            var contextCarrier = new ContextCarrier();
            var span = ContextManager.CreateExitSpan(request.RequestUri.ToString(), contextCarrier, peer);
            try
            {
                Tags.Url.Set(span, request.RequestUri.ToString());
                span.AsHttp();
                span.SetComponent(ComponentsDefine.HttpClient);
                Tags.HTTP.Method.Set(span, request.Method.ToString());
                foreach (var item in contextCarrier.Items)
                    request.Headers.Add(item.HeadKey, item.HeadValue);
                var response = await base.SendAsync(request, cancellationToken);
                Tags.StatusCode.Set(span, response.StatusCode.ToString());
                return response;
            }
            catch (Exception e)
            {
                span.ErrorOccurred().Log(e);
                throw;
            }
            finally
            {
                ContextManager.StopSpan(span);
            }
        }
    }
}
