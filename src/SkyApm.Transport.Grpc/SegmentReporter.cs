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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SkyApm.Config;
using SkyApm.Logging;
using SegmentReporterV5 = SkyApm.Transport.Grpc.V5.SegmentReporter;
using SegmentReporterV6 = SkyApm.Transport.Grpc.V6.SegmentReporter;

namespace SkyApm.Transport.Grpc
{
    public class SegmentReporter : ISegmentReporter
    {
        private readonly ISegmentReporter _segmentReporterV5;
        private readonly ISegmentReporter _segmentReporterV6;
        private readonly TransportConfig _transportConfig;

        public SegmentReporter(ConnectionManager connectionManager, IConfigAccessor configAccessor,
            ILoggerFactory loggerFactory)
        {
            _transportConfig = configAccessor.Get<TransportConfig>();
            _segmentReporterV5 = new V5.SegmentReporter(connectionManager, configAccessor, loggerFactory);
            _segmentReporterV6 = new V6.SegmentReporter(connectionManager, configAccessor, loggerFactory);
        }

        public async Task ReportAsync(IReadOnlyCollection<SegmentRequest> segmentRequests,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_transportConfig.ProtocolVersion == ProtocolVersions.V6)
                await _segmentReporterV6.ReportAsync(segmentRequests, cancellationToken);
            if (_transportConfig.ProtocolVersion == ProtocolVersions.V5)
                await _segmentReporterV5.ReportAsync(segmentRequests, cancellationToken);
        }
    }
}