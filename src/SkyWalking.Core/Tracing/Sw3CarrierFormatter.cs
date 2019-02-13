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

using System.Linq;
using SkyWalking.Common;
using SkyWalking.Config;

namespace SkyWalking.Tracing
{
    public class Sw3CarrierFormatter : ICarrierFormatter
    {
        private readonly IUniqueIdParser _uniqueIdParser;

        public Sw3CarrierFormatter(IUniqueIdParser uniqueIdParser, IConfigAccessor configAccessor)
        {
            _uniqueIdParser = uniqueIdParser;
            var config = configAccessor.Get<InstrumentConfig>();
            Key = string.IsNullOrEmpty(config.Namespace)
                ? HeaderVersions.SW3
                : $"{config.Namespace}-{HeaderVersions.SW3}";
            Enable = config.HeaderVersions != null && config.HeaderVersions.Contains(HeaderVersions.SW3);
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

            var parts = content.Split('|');
            if (parts.Length < 8)
                return Defer();

            if (!_uniqueIdParser.TryParse(parts[0], out var segmentId))
                return Defer();

            if (!int.TryParse(parts[1], out var parentSpanId))
                return Defer();

            if (!int.TryParse(parts[2], out var parentServiceInstanceId))
                return Defer();

            if (!int.TryParse(parts[3], out var entryServiceInstanceId))
                return Defer();

            if (!_uniqueIdParser.TryParse(parts[7], out var traceId))
                return Defer();

            return new Carrier(traceId, segmentId, parentSpanId, parentServiceInstanceId,
                entryServiceInstanceId)
            {
                NetworkAddress = StringOrIntValueHelpers.ParseStringOrIntValue(parts[4]),
                EntryEndpoint = StringOrIntValueHelpers.ParseStringOrIntValue(parts[5]),
                ParentEndpoint = StringOrIntValueHelpers.ParseStringOrIntValue(parts[6])
            };
        }

        public string Encode(ICarrier carrier)
        {
            if (!carrier.HasValue)
                return string.Empty;
            return string.Join("|",
                carrier.ParentSegmentId.ToString(),
                carrier.ParentSpanId.ToString(),
                carrier.ParentServiceInstanceId.ToString(),
                carrier.EntryServiceInstanceId.ToString(),
                ConvertStringOrIntValue(carrier.NetworkAddress),
                ConvertStringOrIntValue(carrier.EntryEndpoint),
                ConvertStringOrIntValue(carrier.ParentEndpoint),
                carrier.TraceId.ToString());
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