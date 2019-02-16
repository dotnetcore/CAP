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

namespace SkyApm.Config
{
    [Config("SkyWalking", "Transport")]
    public class TransportConfig
    {
        public int QueueSize { get; set; } = 30000;

        /// <summary>
        /// Flush Interval Millisecond
        /// </summary>
        public int Interval { get; set; } = 3000;

        /// <summary>
        /// Data queued beyond this time will be discarded.
        /// </summary>
        public int BatchSize { get; set; } = 3000;

        public string ProtocolVersion { get; set; } = ProtocolVersions.V6;
    }

    public static class ProtocolVersions
    {
        public static string V5 { get; } = "v5";
        
        public static string V6 { get; } = "v6";
    }
}