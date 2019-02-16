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

using System.Linq;
using SkyApm.Common;
using SkyApm.Config;

namespace SkyApm.Tracing
{
    public class Sw6CarrierFormatter : ICarrierFormatter
    {
        private readonly IUniqueIdParser _uniqueIdParser;
        private readonly IBase64Formatter _base64Formatter;

        public Sw6CarrierFormatter(IUniqueIdParser uniqueIdParser, IBase64Formatter base64Formatter,
            IConfigAccessor configAccessor)
        {
            _uniqueIdParser = uniqueIdParser;
            _base64Formatter = base64Formatter;
            var config = configAccessor.Get<InstrumentConfig>();
            Key = string.IsNullOrEmpty(config.Namespace)
                ? HeaderVersions.SW6
                : $"{config.Namespace}-{HeaderVersions.SW6}";
            Enable = config.HeaderVersions == null || config.HeaderVersions.Contains(HeaderVersions.SW6);
        }

        public string Key { get; }

        public bool Enable { get; }

        public ICarrier Decode(string content)
        {
            NullableCarrier Defer()
            {
                return NullableCarrier.Instance;
            }

            if (string.IsNullOrEmpty(content))
                return Defer();

            var parts = content.Split('-');
            if (parts.Length < 7)
                return Defer();

            if (!int.TryParse(parts[0], out var sampled))
                return Defer();

            if (!_uniqueIdParser.TryParse(_base64Formatter.Decode(parts[1]), out var traceId))
                return Defer();
            
            if (!_uniqueIdParser.TryParse(_base64Formatter.Decode(parts[2]), out var segmentId))
                return Defer();

            if (!int.TryParse(parts[3], out var parentSpanId))
                return Defer();

            if (!int.TryParse(parts[4], out var parentServiceInstanceId))
                return Defer();

            if (!int.TryParse(parts[5], out var entryServiceInstanceId))
                return Defer();

            var carrier = new Carrier(traceId, segmentId, parentSpanId, parentServiceInstanceId,
                entryServiceInstanceId)
            {
                NetworkAddress = StringOrIntValueHelpers.ParseStringOrIntValue(_base64Formatter.Decode(parts[6])),
                Sampled = sampled != 0
            };

            if (parts.Length >= 9)
            {
                carrier.ParentEndpoint =
                    StringOrIntValueHelpers.ParseStringOrIntValue(_base64Formatter.Decode(parts[7]));
                carrier.EntryEndpoint =
                    StringOrIntValueHelpers.ParseStringOrIntValue(_base64Formatter.Decode(parts[8]));
            }

            return carrier;
        }

        public string Encode(ICarrier carrier)
        {
            if (!carrier.HasValue)
                return string.Empty;
            return string.Join("-",
                carrier.Sampled != null && carrier.Sampled.Value ? "1" : "0",
                _base64Formatter.Encode(carrier.TraceId.ToString()),
                _base64Formatter.Encode(carrier.ParentSegmentId.ToString()),
                carrier.ParentSpanId.ToString(),
                carrier.ParentServiceInstanceId.ToString(),
                carrier.EntryServiceInstanceId.ToString(),
                _base64Formatter.Encode(ConvertStringOrIntValue(carrier.NetworkAddress)),
                _base64Formatter.Encode(ConvertStringOrIntValue(carrier.ParentEndpoint)),
                _base64Formatter.Encode(ConvertStringOrIntValue(carrier.EntryEndpoint)));
        }
        
        private static string ConvertStringOrIntValue(StringOrIntValue value)
        {
            if (value.HasIntValue)
            {
                return value.GetIntValue().ToString();
            }

            return "#" + value.GetStringValue();
        }
    }
}