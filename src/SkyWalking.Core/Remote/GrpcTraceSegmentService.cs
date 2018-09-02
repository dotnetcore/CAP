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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Boot;
using SkyWalking.Config;
using SkyWalking.Context;
using SkyWalking.Context.Trace;
using SkyWalking.Logging;
using SkyWalking.NetworkProtocol;
using SkyWalking.Utils;

namespace SkyWalking.Remote
{
    public class GrpcTraceSegmentService : TimerService, ITracingContextListener
    {
        private static readonly ILogger _logger = LogManager.GetLogger<GrpcTraceSegmentService>();
        private static readonly ConcurrentQueue<ITraceSegment> _traceSegments
            = new ConcurrentQueue<ITraceSegment>();

        public override void Dispose()
        {
            TracingContext.ListenerManager.Remove(this);
            if (_traceSegments.Count > 0)
            {
                BatchSendTraceSegments().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            base.Dispose();
        }

        public override int Order { get; } = 1;

        protected override TimeSpan Interval => TimeSpan.FromSeconds(1);

        protected override Task Initializing(CancellationToken token)
        {
            TracingContext.ListenerManager.Add(this);
            return base.Initializing(token);
        }

        public void AfterFinished(ITraceSegment traceSegment)
        {
            if (traceSegment.IsIgnore)
            {
                return;
            }

            if (_traceSegments.Count >= AgentConfig.PendingSegmentsLimit && AgentConfig.PendingSegmentsLimit > 0)
            {
                _traceSegments.TryDequeue(out var v);
            }
            _traceSegments.Enqueue(traceSegment);
        }

        protected async override Task Execute(CancellationToken token)
        {
            await BatchSendTraceSegments();
        }

        private async Task BatchSendTraceSegments()
        {
            if (_traceSegments.Count == 0)
                return;

            var availableConnection = GrpcConnectionManager.Instance.GetAvailableConnection();
            if (availableConnection == null)
            {
                _logger.Warning(
                    $"Transform and send UpstreamSegment to collector fail. {GrpcConnectionManager.NotFoundErrorMessage}");
                return;
            }

            try
            {
                var traceSegmentService =
                    new TraceSegmentService.TraceSegmentServiceClient(availableConnection.GrpcChannel);
                using (var asyncClientStreamingCall = traceSegmentService.collect())
                {
                    while (_traceSegments.TryDequeue(out var segment))
                    {
                        await asyncClientStreamingCall.RequestStream.WriteAsync(segment.Transform());
                        _logger.Debug(
                            $"Transform and send UpstreamSegment to collector. [TraceSegmentId] = {segment.TraceSegmentId} [GlobalTraceId] = {segment.RelatedGlobalTraces.FirstOrDefault()}");
                    }
                    await asyncClientStreamingCall.RequestStream.CompleteAsync();
                    await asyncClientStreamingCall.ResponseAsync;
                }
            }
            catch (Exception e)
            {
                _logger.Warning($"Transform and send UpstreamSegment to collector fail. {e.Message}");
                availableConnection?.Failure();
                return;
            }
        }
    }
}