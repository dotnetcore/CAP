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

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using SkyApm.Config;

namespace SkyApm.Utilities.Configuration
{
    internal static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddSkyWalkingDefaultConfig(this IConfigurationBuilder builder)
        {
            var defaultLogFile = Path.Combine("logs", "skyapm-{Date}.log");
            var defaultConfig = new Dictionary<string, string>
            {
                {"SkyWalking:Namespace", string.Empty},
                {"SkyWalking:ServiceName", "My_Service"},
                {"SkyWalking:HeaderVersions:0", HeaderVersions.SW6},
                {"SkyWalking:Sampling:SamplePer3Secs", "-1"},
                {"SkyWalking:Sampling:Percentage", "-1"},
                {"SkyWalking:Logging:Level", "Information"},
                {"SkyWalking:Logging:FilePath", defaultLogFile},
                {"SkyWalking:Transport:Interval", "3000"},
                {"SkyWalking:Transport:ProtocolVersion", ProtocolVersions.V6},
                {"SkyWalking:Transport:QueueSize", "30000"},
                {"SkyWalking:Transport:BatchSize", "3000"},
                {"SkyWalking:Transport:gRPC:Servers", "localhost:11800"},
                {"SkyWalking:Transport:gRPC:Timeout", "10000"},
                {"SkyWalking:Transport:gRPC:ReportTimeout", "600000"},
                {"SkyWalking:Transport:gRPC:ConnectTimeout", "10000"}
            };
            return builder.AddInMemoryCollection(defaultConfig);
        }
    }
}