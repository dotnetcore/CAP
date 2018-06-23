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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SkyWalking.Boot;
using SkyWalking.Config;
using SkyWalking.Diagnostics;
using SkyWalking.Logging;
using SkyWalking.Remote;

namespace SkyWalking.AspNetCore
{
    public class SkyWalkingHostedService : IHostedService
    {
        private readonly TracingDiagnosticProcessorObserver _diagnosticObserver;
        private readonly ILogger _logger;

        public SkyWalkingHostedService(IOptions<SkyWalkingOptions> options, IHostingEnvironment hostingEnvironment,
            TracingDiagnosticProcessorObserver diagnosticObserver, ILoggerFactory loggerFactory)
        {

            if (string.IsNullOrEmpty(options.Value.DirectServers))
            {
                throw new ArgumentException("DirectServers cannot be empty or null.");
            }

            if (string.IsNullOrEmpty(options.Value.ApplicationCode))
            {
                options.Value.ApplicationCode = hostingEnvironment.ApplicationName;
            }

            LogManager.SetLoggerFactory(loggerFactory);
            AgentConfig.ApplicationCode = options.Value.ApplicationCode;
            CollectorConfig.DirectServers = options.Value.DirectServers;
            AgentConfig.SamplePer3Secs = options.Value.SamplePer3Secs;
            AgentConfig.PendingSegmentsLimit = options.Value.PendingSegmentsLimit;
            _logger = LogManager.GetLogger<SkyWalkingHostedService>();
            _diagnosticObserver = diagnosticObserver;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info("SkyWalking Agent starting...");
            try
            {
                DiagnosticListener.AllListeners.Subscribe(_diagnosticObserver);
                await GrpcConnectionManager.Instance.ConnectAsync(TimeSpan.FromSeconds(3));
                await ServiceManager.Instance.Initialize();
                _logger.Info("SkyWalking Agent started.");
            }
            catch (Exception e)
            {
                _logger.Error("SkyWalking Agent start fail.", e);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("SkyWalking Agent stopping...");
            try
            {
                ServiceManager.Instance.Dispose();
                await GrpcConnectionManager.Instance.ShutdownAsync();
                _logger.Info("SkyWalking Agent stopped.");
            }
            catch (Exception e)
            {
                _logger.Error("SkyWalking Agent stop fail.", e);
            }

        }
    }
}