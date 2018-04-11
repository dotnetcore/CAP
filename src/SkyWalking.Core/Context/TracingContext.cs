/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
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
using System.Linq;
using System.Runtime.CompilerServices;
using SkyWalking.Boot;
using SkyWalking.Context.Trace;
using SkyWalking.Dictionarys;
using SkyWalking.Sampling;
using SkyWalking.Utils;

namespace SkyWalking.Context
{
    public class TracingContext : ITracerContext
    {
        private long _lastWarningTimestamp = 0;
        private readonly ISampler _sampler;
        private readonly ITraceSegment _segment;
        private readonly Stack<ISpan> _activeSpanStacks;
        private int _spanIdGenerator;

        public TracingContext()
        {
            _sampler = ServiceManager.Instance.GetService<SamplingService>();
            _segment = new TraceSegment();
            _activeSpanStacks = new Stack<ISpan>();
        }

        /// <summary>
        /// Inject the context into the given carrier, only when the active span is an exit one.
        /// </summary>
        public void Inject(IContextCarrier carrier)
        {
            var span = InternalActiveSpan();
            if (!span.IsExit)
            {
                throw new InvalidOperationException("Inject can be done only in Exit Span");
            }

            var spanWithPeer = span as IWithPeerInfo;
            var peer = spanWithPeer.Peer;
            var peerId = spanWithPeer.PeerId;

            carrier.TraceSegmentId = _segment.TraceSegmentId;
            carrier.SpanId = span.SpanId;
            carrier.ParentApplicationInstanceId = _segment.ApplicationInstanceId;

            if (DictionaryUtil.IsNull(peerId))
            {
                carrier.PeerHost = peer;
            }
            else
            {
                carrier.PeerId = peerId;
            }

            var refs = _segment.Refs;
            var firstSpan = _activeSpanStacks.First();

            var metaValue = GetMetaValue(refs);

            carrier.EntryApplicationInstanceId = metaValue.entryApplicationInstanceId;

            if (DictionaryUtil.IsNull(metaValue.operationId))
            {
                carrier.EntryOperationName = metaValue.operationName;
            }
            else
            {
                carrier.EntryOperationId = metaValue.operationId;
            }

            var parentOperationId = firstSpan.OperationId;
            if (DictionaryUtil.IsNull(parentOperationId))
            {
                carrier.ParentOperationName = firstSpan.OperationName;
            }
            else
            {
                carrier.ParentOperationId = parentOperationId;
            }

            carrier.SetDistributedTraceIds(_segment.RelatedGlobalTraces);
        }

        /// <summary>
        /// Extract the carrier to build the reference for the pre segment.
        /// </summary>
        public void Extract(IContextCarrier carrier)
        {
            var traceSegmentRef = new TraceSegmentRef(carrier);
            _segment.Ref(traceSegmentRef);
            _segment.RelatedGlobalTrace(carrier.DistributedTraceId);
            var span = InternalActiveSpan();
            if (span is EntrySpan)
            {
                span.Ref(traceSegmentRef);
            }
        }

        /// <summary>
        /// Capture the snapshot of current context.
        /// </summary>
        public IContextSnapshot Capture => InternalCapture();

        public ISpan ActiveSpan => InternalActiveSpan();

        public void Continued(IContextSnapshot snapshot)
        {
            var segmentRef = new TraceSegmentRef(snapshot);
            _segment.Ref(segmentRef);
            ActiveSpan.Ref(segmentRef);
            _segment.RelatedGlobalTrace(snapshot.DistributedTraceId);
        }

        public string GetReadableGlobalTraceId()
        {
            return _segment.RelatedGlobalTraces.First()?.ToString();
        }

        /// <summary>
        /// Create an entry span
        /// </summary>
        public ISpan CreateEntrySpan(string operationName)
        {
            if (!EnsureLimitMechanismWorking(out var noopSpan))
            {
                return noopSpan;
            }

            _activeSpanStacks.TryPeek(out var parentSpan);
            var parentSpanId = parentSpan?.SpanId ?? -1;

            if (parentSpan != null && parentSpan.IsEntry)
            {
                var entrySpan = (ISpan) DictionaryManager.OperationName.FindOnly(_segment.ApplicationId, operationName)
                    .InCondition(id =>
                    {
                        parentSpan.OperationId = id;
                        return parentSpan;
                    }, () =>
                    {
                        parentSpan.OperationName = operationName;
                        return parentSpan;
                    });
                return entrySpan.Start();
            }
            else
            {
                var entrySpan = (ISpan) DictionaryManager.OperationName.FindOnly(_segment.ApplicationId, operationName)
                    .InCondition(id => new EntrySpan(_spanIdGenerator++, parentSpanId, id),
                        () => new EntrySpan(_spanIdGenerator++, parentSpanId, operationName));

                entrySpan.Start();
                
                _activeSpanStacks.Push(entrySpan);

                return entrySpan;
            }
        }

        /// <summary>
        /// Create a local span
        /// </summary>
        public ISpan CreateLocalSpan(string operationName)
        {
            if (!EnsureLimitMechanismWorking(out var noopSpan))
            {
                return noopSpan;
            }

            _activeSpanStacks.TryPeek(out var parentSpan);

            var parentSpanId = parentSpan?.SpanId ?? -1;

            var span = (ISpan) DictionaryManager.OperationName
                .FindOrPrepareForRegister(_segment.ApplicationId, operationName, false, false)
                .InCondition(id => new LocalSpan(_spanIdGenerator++, parentSpanId, operationName),
                    () => new LocalSpan(_spanIdGenerator++, parentSpanId, operationName));
            span.Start();
            _activeSpanStacks.Push(span);
            return span;
        }

        /// <summary>
        /// Create an exit span
        /// </summary>
        public ISpan CreateExitSpan(string operationName, string remotePeer)
        {
            _activeSpanStacks.TryPeek(out var parentSpan);
            if (parentSpan != null && parentSpan.IsExit)
            {
                return parentSpan.Start();
            }
            else
            {
                var parentSpanId = parentSpan?.SpanId ?? -1;
                var exitSpan = (ISpan) DictionaryManager.NetworkAddress.Find(remotePeer)
                    .InCondition(peerId =>
                        {
                            if (IsLimitMechanismWorking())
                            {
                                return new NoopExitSpan(peerId);
                            }

                            return DictionaryManager.OperationName.FindOnly(_segment.ApplicationId, operationName)
                                .InCondition(id => new ExitSpan(_spanIdGenerator++, parentSpanId, id, peerId),
                                    () => new ExitSpan(_spanIdGenerator++, parentSpanId, operationName, peerId));
                        },
                        () =>
                        {
                            if (IsLimitMechanismWorking())
                            {
                                return new NoopExitSpan(remotePeer);
                            }

                            return DictionaryManager.OperationName.FindOnly(_segment.ApplicationId, operationName)
                                .InCondition(id => new ExitSpan(_spanIdGenerator++, parentSpanId, id, remotePeer),
                                    () => new ExitSpan(_spanIdGenerator++, parentSpanId, operationName,
                                        remotePeer));
                        });
                _activeSpanStacks.Push(exitSpan);
                return exitSpan.Start();
            }
        }

        /// <summary>
        /// Stop the given span, if and only if this one is the top element of {@link #activeSpanStack}. Because the tracing
        /// core must make sure the span must match in a stack module, like any program did.
        /// </summary>
        public void StopSpan(ISpan span)
        {
            _activeSpanStacks.TryPeek(out var lastSpan);
            if (lastSpan == span)
            {
                if (lastSpan is AbstractTracingSpan tracingSpan)
                {
                    if (tracingSpan.Finish(_segment))
                    {
                        _activeSpanStacks.Pop();
                    }
                }
                else
                {
                    _activeSpanStacks.Pop();
                }
                
            }
            else
            {
                throw new InvalidOperationException("Stopping the unexpected span = " + span);
            }

            if (_activeSpanStacks.Count == 0)
            {
                Finish();
            }
        }


        private void Finish()
        {
            var finishedSegment = _segment.Finish(IsLimitMechanismWorking());

            if (!_segment.HasRef && _segment.IsSingleSpanSegment)
            {
                if (!_sampler.TrySampling())
                {
                    finishedSegment.IsIgnore = true;
                }
            }

            ListenerManager.NotifyFinish(finishedSegment);
        }

        private ISpan InternalActiveSpan()
        {
            if (!_activeSpanStacks.TryPeek(out var span))
            {
                throw new InvalidOperationException("No active span.");
            }

            return span;
        }

        private IContextSnapshot InternalCapture()
        {
            var refs = _segment.Refs;

            var snapshot =
                new ContextSnapshot(_segment.TraceSegmentId, ActiveSpan.SpanId, _segment.RelatedGlobalTraces);

            var metaValue = GetMetaValue(refs);

            snapshot.EntryApplicationInstanceId = metaValue.entryApplicationInstanceId;

            if (DictionaryUtil.IsNull(metaValue.operationId))
            {
                snapshot.EntryOperationName = metaValue.operationName;
            }
            else
            {
                snapshot.EntryOperationId = metaValue.operationId;
            }

            var parentSpan = _activeSpanStacks.First();

            if (DictionaryUtil.IsNull(parentSpan.OperationId))
            {
                snapshot.ParentOperationName = parentSpan.OperationName;
            }
            else
            {
                snapshot.ParentOperationId = parentSpan.OperationId;
            }

            return snapshot;
        }

        private (string operationName, int operationId, int entryApplicationInstanceId) GetMetaValue(
            IEnumerable<ITraceSegmentRef> refs)
        {
            if (refs != null && refs.Any())
            {
                var segmentRef = refs.First();
                return (segmentRef.EntryOperationName, segmentRef.EntryOperationId,
                    segmentRef.EntryApplicationInstanceId);
            }
            else
            {
                var span = _activeSpanStacks.First();
                return (span.OperationName, span.OperationId, _segment.ApplicationInstanceId);
            }
        }

        private bool IsLimitMechanismWorking()
        {
            if (_spanIdGenerator < Config.AgentConfig.SpanLimitPerSegment)
            {
                return false;
            }

            var currentTimeMillis = DateTime.UtcNow.GetTimeMillis();
            if (currentTimeMillis - _lastWarningTimestamp > 30 * 1000)
            {
                //todo log warning
                _lastWarningTimestamp = currentTimeMillis;
            }

            return true;
        }

        private bool EnsureLimitMechanismWorking(out ISpan noopSpan)
        {
            if (IsLimitMechanismWorking())
            {
                var span = new NoopSpan();
                _activeSpanStacks.Push(span);
                noopSpan = span;
                return false;
            }

            noopSpan = null;
            
            return true;
        }
        
        
        public static class ListenerManager
        {
            private static readonly IList<ITracingContextListener> _listeners = new List<ITracingContextListener>();


            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Add(ITracingContextListener listener)
            {
                _listeners.Add(listener);
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Remove(ITracingContextListener listener)
            {
                _listeners.Remove(listener);
            }

            public static void NotifyFinish(ITraceSegment traceSegment)
            {
                foreach (var listener in _listeners)
                {
                    listener.AfterFinished(traceSegment);
                }
            }
        }
    }
}