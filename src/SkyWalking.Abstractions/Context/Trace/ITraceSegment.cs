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

using System.Collections.Generic;
using SkyWalking.Transport;
using SkyWalking.Context.Ids;

namespace SkyWalking.Context.Trace
{
    public interface ITraceSegment
    {
        void Archive(AbstractTracingSpan finishedSpan);

        ITraceSegment Finish(bool isSizeLimited);

        int ApplicationId { get; }

        int ApplicationInstanceId { get; }

        IEnumerable<ITraceSegmentRef> Refs { get; }

        IEnumerable<DistributedTraceId> RelatedGlobalTraces { get; }

        ID TraceSegmentId { get; }

        bool HasRef { get; }

        bool IsIgnore { get; set; }

        bool IsSingleSpanSegment { get; }

        void Ref(ITraceSegmentRef refSegment);

        void RelatedGlobalTrace(DistributedTraceId distributedTraceId);

        SegmentRequest Transform();
    }
}
