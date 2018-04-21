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

using System;
using System.Collections;
using System.Collections.Generic;
using SkyWalking.Config;

namespace SkyWalking.Context
{
    public class CarrierItem : IEnumerable<CarrierItem>
    {
        private string _headKey;
        private string _headValue;
        private CarrierItem _next;

        public virtual string HeadKey
        {
            get
            {
                return _headKey;
            }
        }

        public virtual string HeadValue
        {
            get
            {
                return _headValue;
            }
            set
            {
                _headValue = value;
            }
        }

        public CarrierItem(String headKey, String headValue)
        {
            _headKey = headKey;
            _headValue = headValue;
            _next = null;
        }

        public CarrierItem(String headKey, String headValue, CarrierItem next)
        {
            if (string.IsNullOrEmpty(AgentConfig.Namespace))
            {
                _headKey = headKey;
            }
            else
            {
                _headKey = $"{AgentConfig.Namespace}:{headKey}";
            }
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
            private CarrierItem _head;
            private CarrierItem _current;

            public CarrierItem Current => _current;

            object IEnumerator.Current => _current;

            public Enumerator(CarrierItem head)
            {
                _head = head;
                _current = head;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var next = _current._next;
                if (next == null)
                {
                    return false;
                }
                _current = next;
                return true;
            }

            public void Reset()
            {
                _current = _head;
            }
        }
    }
}
