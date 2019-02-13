/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The OpenSkywalking licenses this file to You under the Apache License, Version 2.0
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
using System.Threading;
using SkyWalking.Tracing.Segments;

namespace SkyWalking.Tracing
{
    public class LocalSegmentContextAccessor : ILocalSegmentContextAccessor
    {
        private readonly AsyncLocal<ConcurrentStack<SegmentContext>> _segmentContextStack =
            new AsyncLocal<ConcurrentStack<SegmentContext>>();

        public SegmentContext Context
        {
            get
            {
                var stack = _segmentContextStack.Value;
                if (stack == null)
                {
                    return null;
                }
                stack.TryPeek(out var context);
                return context;
            }
            set
            {
                var stack = _segmentContextStack.Value;
                if (stack == null)
                {
                    if (value == null) return;
                    stack = new ConcurrentStack<SegmentContext>();
                    stack.Push(value);
                    _segmentContextStack.Value = stack;
                }
                else
                {
                    if (value == null)
                    {
                        stack.TryPop(out _);
                        if (stack.IsEmpty)
                        {
                            _segmentContextStack.Value = null;
                        }
                    }
                    else
                    {
                        stack.Push(value);
                    }
                }
            }
        }
    }
}