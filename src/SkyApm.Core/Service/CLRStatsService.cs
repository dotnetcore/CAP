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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SkyApm.Common;
using SkyApm.Logging;
using SkyApm.Transport;

namespace SkyApm.Service
{
    public class CLRStatsService : ExecutionService
    {
        private readonly ICLRStatsReporter _reporter;

        public CLRStatsService(ICLRStatsReporter reporter, IRuntimeEnvironment runtimeEnvironment,
            ILoggerFactory loggerFactory)
            : base(runtimeEnvironment, loggerFactory)
        {
            _reporter = reporter;
        }

        protected override TimeSpan DueTime { get; } = TimeSpan.FromSeconds(30);
        protected override TimeSpan Period { get; } = TimeSpan.FromSeconds(30);

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var cpuStats = new CPUStatsRequest
            {
                UsagePercent = CpuHelpers.UsagePercent
            };
            var gcStats = new GCStatsRequest
            {
                Gen0CollectCount = GCHelpers.Gen0CollectCount,
                Gen1CollectCount = GCHelpers.Gen1CollectCount,
                Gen2CollectCount = GCHelpers.Gen2CollectCount,
                HeapMemory = GCHelpers.TotalMemory
            };
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
            var threadStats = new ThreadStatsRequest
            {
                MaxCompletionPortThreads = maxCompletionPortThreads,
                MaxWorkerThreads = maxWorkerThreads,
                AvailableCompletionPortThreads = availableCompletionPortThreads,
                AvailableWorkerThreads = availableWorkerThreads
            };
            var statsRequest = new CLRStatsRequest
            {
                CPU = cpuStats,
                GC = gcStats,
                Thread = threadStats
            };
            try
            {
                await _reporter.ReportAsync(statsRequest, cancellationToken);
                Logger.Information(
                    $"Report CLR Stats. CPU UsagePercent {cpuStats.UsagePercent} GenCollectCount {gcStats.Gen0CollectCount} {gcStats.Gen1CollectCount} {gcStats.Gen2CollectCount} {gcStats.HeapMemory / (1024 * 1024)}M ThreadPool {threadStats.AvailableWorkerThreads} {threadStats.MaxWorkerThreads} {threadStats.AvailableCompletionPortThreads} {threadStats.MaxCompletionPortThreads}");
            }
            catch (Exception exception)
            {
                Logger.Error("Report CLR Stats error.", exception);
            }
        }
    }
}