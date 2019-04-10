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

namespace SkyApm.Common
{
    public struct StringOrIntValue
    {
        private readonly int _intValue;
        private readonly string _stringValue;

        public StringOrIntValue(int value)
        {
            _intValue = value;
            _stringValue = null;
        }

        public bool HasValue => HasIntValue || HasStringValue;

        public bool HasIntValue => _intValue != 0;

        public bool HasStringValue => _stringValue != null;

        public StringOrIntValue(string value)
        {
            _intValue = 0;
            _stringValue = value;
        }

        public StringOrIntValue(int intValue, string stringValue)
        {
            _intValue = intValue;
            _stringValue = stringValue;
        }

        public int GetIntValue() => _intValue;

        public string GetStringValue() => _stringValue;

        public (string, int) GetValue()
        {
            return (_stringValue, _intValue);
        }

        public override string ToString()
        {
            if (HasIntValue) return _intValue.ToString();
            return _stringValue;
        }

        public static implicit operator StringOrIntValue(string value) => new StringOrIntValue(value);
        public static implicit operator StringOrIntValue(int value) => new StringOrIntValue(value);
    }
}
