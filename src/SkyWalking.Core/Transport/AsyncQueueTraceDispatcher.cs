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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Config;
using SkyWalking.Logging;

namespace SkyWalking.Transport
{
    public class AsyncQueueTraceDispatcher : ITraceDispatcher
    {
        private readonly ILogger _logger;
        private readonly TransportConfig _config;
        private readonly ISkyWalkingClient _skyWalkingClient;
        private readonly ConcurrentQueue<TraceSegmentRequest> _segmentQueue;
        private readonly CancellationTokenSource _cancellation;

        public AsyncQueueTraceDispatcher(IConfigAccessor configAccessor, ISkyWalkingClient client, ILoggerFactory loggerFactory)
        {
            _skyWalkingClient = client;
            _logger = loggerFactory.CreateLogger(typeof(AsyncQueueTraceDispatcher));
            _config = configAccessor.Get<TransportConfig>();
            _segmentQueue = new ConcurrentQueue<TraceSegmentRequest>();
            _cancellation = new CancellationTokenSource();
        }

        public bool Dispatch(TraceSegmentRequest segment)
        {
            // todo performance optimization for ConcurrentQueue
            if (_config.PendingSegmentLimit < _segmentQueue.Count || _cancellation.IsCancellationRequested)
            {
                return false;
            }

            _segmentQueue.Enqueue(segment);

            _logger.Debug($"Dispatch trace segment. [SegmentId]={segment.Segment.SegmentId}.");
            return true;
        }

        public Task Flush(CancellationToken token = default(CancellationToken))
        {
            // todo performance optimization for ConcurrentQueue
            //var queued = _segmentQueue.Count;
            //var limit = queued <= _config.PendingSegmentLimit ? queued : _config.PendingSegmentLimit;
            var limit = _config.PendingSegmentLimit;
            var index = 0;
            var segments = new List<TraceSegmentRequest>(limit);
            while (index++ < limit && _segmentQueue.TryDequeue(out var request))
            {
                segments.Add(request);
            }

            // send async
            if (segments.Count > 0)
                _skyWalkingClient.CollectAsync(segments, token);
            return Task.CompletedTask;
        }

        public void Close()
        {
            _cancellation.Cancel();
        }
    }
}