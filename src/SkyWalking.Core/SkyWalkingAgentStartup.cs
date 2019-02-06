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
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Diagnostics;
using SkyWalking.Logging;

namespace SkyWalking
{
    public class SkyWalkingAgentStartup : ISkyWalkingAgentStartup
    {
        private readonly TracingDiagnosticProcessorObserver _observer;
        private readonly IEnumerable<IExecutionService> _services;
        private readonly ILogger _logger;

        public SkyWalkingAgentStartup(TracingDiagnosticProcessorObserver observer, IEnumerable<IExecutionService> services, ILoggerFactory loggerFactory)
        {
            _observer = observer;
            _services = services;
            _logger = loggerFactory.CreateLogger(typeof(SkyWalkingAgentStartup));
        }

        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.Information("Initializing ...");
            foreach (var service in _services)
                await service.StartAsync(cancellationToken);
            DiagnosticListener.AllListeners.Subscribe(_observer);
            _logger.Information("Started SkyWalking .NET Core Agent.");
        }

        public async Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var service in _services)
                await service.StopAsync(cancellationToken);
            _logger.Information("Stopped SkyWalking .NET Core Agent.");
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        private string Welcome()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Initializing ...");
            builder.AppendLine();
            builder.AppendLine("***************************************************************");
            builder.AppendLine("*                                                             *");
            builder.AppendLine("*                Welcome to Apache SkyWalking                 *");
            builder.AppendLine("*                                                             *");
            builder.AppendLine("***************************************************************");
            return builder.ToString();
        }
    }
}