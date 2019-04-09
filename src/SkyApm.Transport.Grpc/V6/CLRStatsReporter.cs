/*
 * Licensed to the SkyAPM under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The SkyAPM licenses this file to You under the Apache License, Version 2.0
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
using System.Threading;
using System.Threading.Tasks;
using SkyApm.Config;
using SkyApm.Logging;
using SkyWalking.NetworkProtocol;

namespace SkyApm.Transport.Grpc.V6
{
    public class CLRStatsReporter : ICLRStatsReporter
    {
        private readonly ConnectionManager _connectionManager;
        private readonly ILogger _logger;
        private readonly GrpcConfig _config;
        private readonly IRuntimeEnvironment _runtimeEnvironment;

        public CLRStatsReporter(ConnectionManager connectionManager, ILoggerFactory loggerFactory,
            IConfigAccessor configAccessor, IRuntimeEnvironment runtimeEnvironment)
        {
            _connectionManager = connectionManager;
            _logger = loggerFactory.CreateLogger(typeof(CLRStatsReporter));
            _config = configAccessor.Get<GrpcConfig>();
            _runtimeEnvironment = runtimeEnvironment;
        }

        public async Task ReportAsync(CLRStatsRequest statsRequest,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_connectionManager.Ready)
            {
                return;
            }

            var connection = _connectionManager.GetConnection();

            try
            {
                var request = new CLRMetricCollection
                {
                    ServiceInstanceId = _runtimeEnvironment.ServiceInstanceId.Value
                };
                var metric = new CLRMetric
                {
                    Cpu = new CPU
                    {
                        UsagePercent = statsRequest.CPU.UsagePercent
                    },
                    Gc = new ClrGC
                    {
                        Gen0CollectCount = statsRequest.GC.Gen0CollectCount,
                        Gen1CollectCount = statsRequest.GC.Gen1CollectCount,
                        Gen2CollectCount = statsRequest.GC.Gen2CollectCount,
                        HeapMemory = statsRequest.GC.HeapMemory
                    },
                    Thread = new ClrThread
                    {
                        AvailableWorkerThreads = statsRequest.Thread.MaxWorkerThreads,
                        AvailableCompletionPortThreads = statsRequest.Thread.MaxCompletionPortThreads,
                        MaxWorkerThreads = statsRequest.Thread.MaxWorkerThreads,
                        MaxCompletionPortThreads = statsRequest.Thread.MaxCompletionPortThreads
                    },
                    Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                request.Metrics.Add(metric);
                var client = new CLRMetricReportService.CLRMetricReportServiceClient(connection);
                await client.collectAsync(request, null, _config.GetTimeout(), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.Warning("Report CLR Stats error. " + e);
            }
        }
    }
}