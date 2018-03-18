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
using SkyWalking.Dictionarys;
using SkyWalking.NetworkProtocol;
using SkyWalking.NetworkProtocol.Trace;
using System.Linq;

namespace SkyWalking.Context.Trace
{
    /// <summary>
    /// The <code>AbstractTracingSpan</code> represents a group of {@link ISpan} implementations, which belongs a real distributed trace.
    /// </summary>
    public abstract class AbstractTracingSpan : ISpan
    {
        protected int _spanId;
        protected int _parnetSpanId;
        protected Dictionary<string, string> _tags;
        protected string _operationName;
        protected int _operationId;
        protected SpanLayer? _layer;

        /// <summary>
        /// The start time of this Span.
        /// </summary>
        protected long _startTime;

        /// <summary>
        /// The end time of this Span.
        /// </summary>
        protected long _endTime;

        protected bool _errorOccurred = false;

        protected int _componentId = 0;

        protected string _componentName;

        /// <summary>
        /// Log is a concept from OpenTracing spec. <p> {@see https://github.com/opentracing/specification/blob/master/specification.md#log-structured-data}
        /// </summary>
        protected ICollection<LogDataEntity> _logs;

        /// <summary>
        /// The refs of parent trace segments, except the primary one. For most RPC call, {@link #refs} contains only one
        /// element, but if this segment is a start span of batch process, the segment faces multi parents, at this moment,
        /// we use this {@link #refs} to link them.
        /// </summary>
        protected ICollection<ITraceSegmentRef> _refs;

        protected AbstractTracingSpan(int spanId, int parentSpanId, string operationName)
        {
            _operationName = operationName;
            _operationId = DictionaryUtil.NullValue;
            _spanId = spanId;
            _parnetSpanId = parentSpanId;
        }

        protected AbstractTracingSpan(int spanId, int parentSpanId, int operationId)
        {
            _operationName = null;
            _operationId = operationId;
            _spanId = spanId;
            _parnetSpanId = parentSpanId;
        }

        public abstract bool IsEntry { get; }

        public abstract bool IsExit { get; }

        public virtual int SpanId => _spanId;

        public virtual string OperationName
        {
            get
            {
                return _operationName;
            }
            set
            {
                _operationName = value;
                _operationId = DictionaryUtil.NullValue;
            }
        }

        public virtual int OperationId
        {
            get
            {
                return _operationId;
            }
            set
            {
                _operationId = value;
                _operationName = null;
            }
        }

        public virtual ISpan SetComponent(IComponent component)
        {
            _componentId = component.Id;
            return this;
        }

        public virtual ISpan SetComponent(string componentName)
        {
            _componentName = componentName;
            return this;
        }

        public virtual ISpan Tag(string key, string value)
        {
            if (_tags == null)
            {
                _tags = new Dictionary<string, string>();
            }
            _tags.Add(key, value);
            return this;
        }

        public virtual ISpan SetLayer(SpanLayer layer)
        {
            _layer = layer;
            return this;
        }

        /// <summary>
        /// Record an exception event of the current walltime timestamp.
        /// </summary>
        public virtual ISpan Log(Exception exception)
        {
            EnsureLogs();
            _logs.Add(new LogDataEntity.Builder()
                .Add("event", "error")
                .Add("error.kind", exception.GetType().FullName)
                .Add("message", exception.Message)
                .Add("stack", exception.StackTrace)
                .Build(DateTime.UtcNow.GetTimeMillis()));
            return this;
        }

        public virtual ISpan ErrorOccurred()
        {
            _errorOccurred = true;
            return this;
        }

        public virtual ISpan Log(long timestamp, IDictionary<string, object> events)
        {
            EnsureLogs();
            LogDataEntity.Builder builder = new LogDataEntity.Builder();
            foreach (var @event in events)
            {
                builder.Add(@event.Key, @event.Value.ToString());
            }
            _logs.Add(builder.Build(timestamp));
            return this;
        }

        public virtual ISpan Start()
        {
            _startTime = DateTime.UtcNow.GetTimeMillis();
            return this;
        }

        public virtual ISpan Start(long timestamp)
        {
            _startTime = timestamp;
            return this;
        }

        public virtual void Ref(ITraceSegmentRef traceSegmentRef)
        {
            if (_refs == null)
            {
                _refs = new List<ITraceSegmentRef>();
            }
            if (!_refs.Contains(traceSegmentRef))
            {
                _refs.Add(traceSegmentRef);
            }
        }

        /// <summary>
        /// Finish the active Span. When it is finished, it will be archived by the given {@link TraceSegment}, which owners it
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public virtual bool Finish(ITraceSegment owner)
        {
            _endTime = DateTime.UtcNow.GetTimeMillis();
            owner.Archive(this);
            return true;
        }

        public virtual SpanObject Transform()
        {
            SpanObject spanObject = new SpanObject();

            spanObject.SpanId = _spanId;
            spanObject.ParentSpanId = _parnetSpanId;
            spanObject.StartTime = _startTime;
            spanObject.EndTime = _endTime;

            if (_operationId != DictionaryUtil.NullValue)
            {
                spanObject.OperationNameId = _operationId;
            }
            else
            {
                spanObject.OperationName = _operationName;
            }

            if (IsEntry)
            {
                spanObject.SpanType = SpanType.Entry;
            }
            else if (IsExit)
            {
                spanObject.SpanType = SpanType.Exit;
            }
            else
            {
                spanObject.SpanType = SpanType.Local;
            }

            if (_layer.HasValue)
            {
                spanObject.SpanLayer = (NetworkProtocol.SpanLayer)((int)_layer.Value);
            }

            if (_componentId != DictionaryUtil.NullValue)
            {
                spanObject.ComponentId = _componentId;
            }
            else
            {
                spanObject.Component = _componentName;
            }

            spanObject.IsError = _errorOccurred;

            if (_tags != null)
            {
                spanObject.Tags.Add(_tags.Select(x => new KeyWithStringValue { Key = x.Key, Value = x.Value }));
            }

            if (_logs != null)
            {
                spanObject.Logs.Add(_logs.Select(x => x.Transform()));
            }

            if (_refs != null)
            {
                spanObject.Refs.Add(_refs.Select(x => x.Transform()));
            }

            return spanObject;
        }

        private void EnsureLogs()
        {
            if (_logs == null)
            {
                _logs = new LinkedList<LogDataEntity>();
            }
        }
    }
}
