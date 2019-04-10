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

using SkyApm.Tracing.Segments;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SkyApm.Tracing
{
    public class LocalSegmentContextAccessor : ILocalSegmentContextAccessor
    {
        private readonly ConditionalWeakTable<SegmentContext, SegmentContext> _parent = new ConditionalWeakTable<SegmentContext, SegmentContext>();
        private readonly AsyncLocal<SegmentContext> _segmentContext = new AsyncLocal<SegmentContext>();

        public SegmentContext Context
        {
            get => _segmentContext.Value;
            set
            {
                var current = _segmentContext.Value;
                if (value == null)
                {
                    if (_parent.TryGetValue(current, out var parent))
                        _segmentContext.Value = parent;
                }
                else
                {
                    _parent.Add(value, current);
                    _segmentContext.Value = value;
                }
            }
        }
    }
}
