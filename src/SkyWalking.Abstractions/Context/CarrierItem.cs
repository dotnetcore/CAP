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

using System.Collections;
using System.Collections.Generic;

namespace SkyWalking.Context
{
    public class CarrierItem : IEnumerable<CarrierItem>
    {
        private readonly string _headKey;
        private string _headValue;
        private readonly CarrierItem _next;

        public virtual string HeadKey => _headKey;

        public virtual string HeadValue
        {
            get => _headValue;
            set => _headValue = value;
        }

        protected CarrierItem(string headKey, string headValue, string @namespace)
            : this(headKey, headValue, null, @namespace)
        {
        }

        protected CarrierItem(string headKey, string headValue, CarrierItem next, string @namespace)
        {
            _headKey = string.IsNullOrEmpty(@namespace) ? headKey : $"{@namespace}-{headKey}";
            _headValue = headValue;
            _next = next;
        }

        public IEnumerator<CarrierItem> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Enumerator : IEnumerator<CarrierItem>
        {
            private readonly CarrierItem _head;

            public CarrierItem Current { get; private set; }

            object IEnumerator.Current => Current;

            public Enumerator(CarrierItem head)
            {
                _head = head;
                Current = head;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var next = Current._next;
                if (next == null)
                {
                    return false;
                }

                Current = next;
                return true;
            }

            public void Reset()
            {
                Current = _head;
            }
        }
    }
}