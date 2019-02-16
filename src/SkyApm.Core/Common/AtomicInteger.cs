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

using System.Threading;

namespace SkyApm.Common
{
    public class AtomicInteger
    {
        private int _value;

        public int Value
        {
            get => _value;
            set => Interlocked.Exchange(ref _value, value);
        }

        public AtomicInteger()
            : this(0)
        {
        }

        public AtomicInteger(int defaultValue)
        {
            _value = defaultValue;
        }

        public int Increment()
        {
            Interlocked.Increment(ref _value);
            return _value;
        }

        public int Decrement()
        {
            Interlocked.Decrement(ref _value);
            return _value;
        }

        public int Add(int value)
        {
            AddInternal(value);
            return _value;
        }

        private void AddInternal(int value)
        {
            Interlocked.Add(ref _value, value);
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case AtomicInteger atomicInteger:
                    return atomicInteger._value == _value;
                case int value:
                    return value == _value;
                default:
                    return false;
            }
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static AtomicInteger operator +(AtomicInteger atomicInteger, int value)
        {
            atomicInteger.AddInternal(value);
            return atomicInteger;
        }

        public static AtomicInteger operator +(int value, AtomicInteger atomicInteger)
        {
            atomicInteger.AddInternal(value);
            return atomicInteger;
        }

        public static AtomicInteger operator -(AtomicInteger atomicInteger, int value)
        {
            atomicInteger.AddInternal(-value);
            return atomicInteger;
        }

        public static AtomicInteger operator -(int value, AtomicInteger atomicInteger)
        {
            atomicInteger.AddInternal(-value);
            return atomicInteger;
        }

        public static implicit operator AtomicInteger(int value)
        {
            return new AtomicInteger(value);
        }

        public static implicit operator int(AtomicInteger atomicInteger)
        {
            return atomicInteger._value;
        }
        
        public static bool operator ==(AtomicInteger atomicInteger, int value)
        {
            return atomicInteger._value == value;
        }

        public static bool operator !=(AtomicInteger atomicInteger, int value)
        {
            return !(atomicInteger == value);
        }

        public static bool operator ==(int value, AtomicInteger atomicInteger)
        {
            return atomicInteger._value == value;
        }

        public static bool operator !=(int value, AtomicInteger atomicInteger)
        {
            return !(value == atomicInteger);
        }
    }
}