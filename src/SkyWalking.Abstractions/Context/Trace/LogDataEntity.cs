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
using System.Collections.ObjectModel;
using SkyWalking.NetworkProtocol;

namespace SkyWalking.Context.Trace
{
    public class LogDataEntity
    {
        private long _timestamp = 0;
        private Dictionary<string, string> _logs;

        private LogDataEntity(long timestamp, Dictionary<string, string> logs)
        {
            _timestamp = timestamp;
            _logs = logs;
        }

        public IReadOnlyDictionary<string, string> Logs
        {
            get { return new ReadOnlyDictionary<string, string>(_logs); }
        }

        public class Builder
        {
            private Dictionary<string, string> _logs;

            public Builder()
            {
                _logs = new Dictionary<string, string>();
            }

            public Builder Add(IDictionary<string, string> fields)
            {
                foreach (var field in fields)
                    _logs.Add(field.Key, field.Value);
                return this;
            }

            public Builder Add(string key, string value)
            {
                _logs.Add(key, value);
                return this;
            }

            public LogDataEntity Build(long timestamp)
            {
                return new LogDataEntity(timestamp, _logs);
            }
        }

        public LogMessage Transform()
        {
            LogMessage logMessage = new LogMessage();
            logMessage.Time = _timestamp;
            foreach (var log in _logs)
            {
                logMessage.Data.Add(new KeyWithStringValue {Key = log.Key, Value = log.Value});
            }

            return logMessage;
        }
    }
}
