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
using SkyWalking.Config;

namespace SkyWalking.Transport.Grpc
{
    [Config("SkyWalking", "Transport","gRPC")]
    public class GrpcConfig
    {
        public string Servers { get; set; }
        
        public int ConnectTimeout { get; set; }
        
        public int Timeout { get; set; }
    }

    public static class GrpcConfigExtensions
    {
        public static DateTime GetTimeout(this GrpcConfig config)
        {
            return DateTime.UtcNow.AddMilliseconds(config.Timeout);
        }
    }
}