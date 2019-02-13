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

using System.Collections;
using System.Collections.Generic;
using System.Web;
using SkyWalking.Tracing;

namespace SkyWalking.AspNet
{
    public class HttpRequestCarrierHeaderCollection : ICarrierHeaderCollection
    {
        private readonly Dictionary<string, string> _headers;

        public HttpRequestCarrierHeaderCollection(HttpRequest httpRequest)
        {
            _headers = new Dictionary<string, string>();
            foreach (var key in httpRequest.Headers.AllKeys)
            {
                _headers[key] = httpRequest.Headers[key];
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _headers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _headers.GetEnumerator();
        }

        public void Add(string key, string value)
        {
            throw new System.NotImplementedException();
        }
    }
}