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

using Microsoft.Extensions.DependencyInjection;
using SkyWalking.Config;
using SkyWalking.Context;
using SkyWalking.Diagnostics;
using SkyWalking.Logging;
using SkyWalking.Service;
using SkyWalking.Transport;
using SkyWalking.Transport.Grpc;
using SkyWalking.Transport.Grpc.V5;
using SkyWalking.Transport.Grpc.V6;
using SkyWalking.Utilities.Configuration;
using SkyWalking.Utilities.Logging;

namespace SkyWalking.AspNet.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSkyWalkingCore(this IServiceCollection services)
        {
            services.AddSingleton<IContextCarrierFactory, ContextCarrierFactory>();
            services.AddSingleton<ISegmentDispatcher, AsyncQueueSegmentDispatcher>();
            services.AddSingleton<IExecutionService, SegmentReportService>();
            services.AddSingleton<IExecutionService, RegisterService>();
            services.AddSingleton<IExecutionService, PingService>();
            services.AddSingleton<IExecutionService, SamplingRefreshService>();
            services.AddSingleton<IExecutionService, ServiceDiscoveryV5Service>();
            services.AddSingleton<ISkyWalkingAgentStartup, SkyWalkingAgentStartup>();
            services.AddSingleton<ISampler>(DefaultSampler.Instance);
            services.AddSingleton<IRuntimeEnvironment>(RuntimeEnvironment.Instance);
            services.AddSingleton<TracingDiagnosticProcessorObserver>();
            services.AddSingleton<IConfigAccessor, ConfigAccessor>();
            services.AddSingleton<IEnvironmentProvider, HostingEnvironmentProvider>();
            services.AddSingleton<ISegmentReporter, SegmentReporter>();
            services.AddSingleton<ConnectionManager>();
            services.AddSingleton<IPingCaller, PingCaller>();
            services.AddSingleton<IServiceRegister, ServiceRegister>();
            services.AddSingleton<IExecutionService, ConnectService>();
            services.AddSingleton<ILoggerFactory, DefaultLoggerFactory>();
            services.AddSingleton<ISkyWalkingClientV5, SkyWalkingClientV5>();
            return services;
        }
    }
}