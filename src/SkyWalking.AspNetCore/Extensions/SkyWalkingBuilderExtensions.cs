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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using SkyWalking.AspNetCore.Diagnostics;
using SkyWalking.AspNetCore.Logging;
using SkyWalking.Diagnostics;
using SkyWalking.Extensions.DependencyInjection;
using SkyWalking.Logging;

namespace SkyWalking.AspNetCore
{
    internal static class SkyWalkingBuilderExtensions
    {
        public static SkyWalkingBuilder AddHosting(this SkyWalkingBuilder builder)
        {   
            builder.Services.AddSingleton<IHostedService, SkyWalkingHostedService>();
            builder.Services.AddSingleton<ITracingDiagnosticProcessor, HostingDiagnosticProcessor>();
            builder.Services.AddSingleton<ILoggerFactory, LoggerFactoryAdapter>();
            return builder;
        }

        public static SkyWalkingBuilder AddDiagnostics(this SkyWalkingBuilder builder)
        {
            builder.Services.AddSingleton<TracingDiagnosticProcessorObserver>();
            return builder;
        }

        public static SkyWalkingBuilder AddHttpClientFactory(this SkyWalkingBuilder builder)
        {
            builder.Services.AddHttpClient<TracingHttpClient>();         
            builder.Services.AddTransient<HttpMessageHandlerBuilder, TracingHttpMessageHandlerBuilder>();
            return builder;
        }
    }
}