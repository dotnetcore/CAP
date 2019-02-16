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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SkyApm.Common;

namespace SkyApm.Tracing.Segments
{
    public class SegmentSpan
    {
        public int SpanId { get; } = 0;

        public int ParentSpanId { get; } = -1;

        public long StartTime { get; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public long EndTime { get; private set; }

        public StringOrIntValue OperationName { get; }

        public StringOrIntValue Peer { get; set; }

        public SpanType SpanType { get; }

        public SpanLayer SpanLayer { get; set; }

        public StringOrIntValue Component { get; set; }

        public bool IsError { get; set; }
        public TagCollection Tags { get; } = new TagCollection();

        public LogCollection Logs { get; } = new LogCollection();

        public SegmentSpan(string operationName, SpanType spanType)
        {
            OperationName = new StringOrIntValue(operationName);
            SpanType = spanType;
        }

        public SegmentSpan AddTag(string key, string value)
        {
            Tags.AddTag(key, value);
            return this;
        }

        public SegmentSpan AddTag(string key, long value)
        {
            Tags.AddTag(key, value.ToString());
            return this;
        }

        public SegmentSpan AddTag(string key, bool value)
        {
            Tags.AddTag(key, value.ToString());
            return this;
        }

        public void AddLog(params LogEvent[] events)
        {
            var log = new SpanLog(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), events);
            Logs.AddLog(log);
        }

        public void Finish()
        {
            EndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    public class TagCollection : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly Dictionary<string, string> tags = new Dictionary<string, string>();

        internal void AddTag(string key, string value)
        {
            tags[key] = value;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return tags.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return tags.GetEnumerator();
        }
    }

    public enum SpanType
    {
        Entry = 0,
        Exit = 1,
        Local = 2
    }

    public enum SpanLayer
    {
        DB = 1,
        RPC_FRAMEWORK = 2,
        HTTP = 3,
        MQ = 4,
        CACHE = 5
    }

    public class LogCollection : IEnumerable<SpanLog>
    {
        private readonly List<SpanLog> _logs = new List<SpanLog>();

        internal void AddLog(SpanLog log)
        {
            _logs.Add(log);
        }

        public IEnumerator<SpanLog> GetEnumerator()
        {
            return _logs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _logs.GetEnumerator();
        }
    }

    public class SpanLog
    {
        private static readonly Dictionary<string, string> Empty = new Dictionary<string, string>();
        public long Timestamp { get; }

        public IReadOnlyDictionary<string, string> Data { get; }

        public SpanLog(long timestamp, params LogEvent[] events)
        {
            Timestamp = timestamp;
            Data = events?.ToDictionary(x => x.Key, x => x.Value) ?? Empty;
        }
    }

    public class LogEvent
    {
        public string Key { get; }

        public string Value { get; }

        public LogEvent(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public static LogEvent Event(string value)
        {
            return new LogEvent("event", value);
        }

        public static LogEvent Message(string value)
        {
            return new LogEvent("message", value);
        }

        public static LogEvent ErrorKind(string value)
        {
            return new LogEvent("error.kind", value);
        }

        public static LogEvent ErrorStack(string value)
        {
            return new LogEvent("stack", value);
        }
    }
}