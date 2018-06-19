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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Boot;
using SkyWalking.Context;
using SkyWalking.Context.Trace;
using SkyWalking.Logging;
using SkyWalking.NetworkProtocol;
using SkyWalking.Utils;

namespace SkyWalking.Remote
{
    public class GrpcTraceSegmentService : IBootService, ITracingContextListener
    {
        private static readonly ILogger _logger = LogManager.GetLogger<GrpcTraceSegmentService>();

        public void Dispose()
        {
            TracingContext.ListenerManager.Remove(this);
        }

        public int Order { get; } = 1;

        public Task Initialize(CancellationToken token)
        {
            TracingContext.ListenerManager.Add(this);
            return TaskUtils.CompletedTask;
        }

        public async void AfterFinished(ITraceSegment traceSegment)
        {
            if (traceSegment.IsIgnore)
            {
                return;
            }

            var availableConnection = GrpcConnectionManager.Instance.GetAvailableConnection();
            if (availableConnection == null)
            {
                _logger.Warning(
                    $"Transform and send UpstreamSegment to collector fail. {GrpcConnectionManager.NotFoundErrorMessage}");
                return;
            }

            try
            {
                var segment = traceSegment.Transform();
                var traceSegmentService =
                    new TraceSegmentService.TraceSegmentServiceClient(availableConnection.GrpcChannel);
                using (var asyncClientStreamingCall = traceSegmentService.collect())
                {
                    await asyncClientStreamingCall.RequestStream.WriteAsync(segment);
                    await asyncClientStreamingCall.RequestStream.CompleteAsync();
                    await asyncClientStreamingCall.ResponseAsync;
                }

                _logger.Debug(
                    $"Transform and send UpstreamSegment to collector. [TraceSegmentId] = {traceSegment.TraceSegmentId} [GlobalTraceId] = {traceSegment.RelatedGlobalTraces.FirstOrDefault()}");
            }
            catch (Exception e)
            {
                _logger.Warning($"Transform and send UpstreamSegment to collector fail. {e.Message}");
                availableConnection?.Failure();
            }
        }
    }
}