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

using System.Collections;
using System.Collections.Generic;

namespace SkyApm.Tracing
{
    public class TextCarrierHeaderCollection : ICarrierHeaderCollection
    {
        private readonly IDictionary<string, string> _headers;

        public TextCarrierHeaderCollection(IEnumerable<KeyValuePair<string, string>> headers)
        {
            _headers = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                _headers[header.Key] = header.Value;
            }
        }

        public TextCarrierHeaderCollection(IDictionary<string, string> headers)
        {
            _headers = headers;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _headers.GetEnumerator();
        }

        public void Add(string key, string value)
        {
            _headers[key] = value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _headers.GetEnumerator();
        }
    }
}