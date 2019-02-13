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

namespace SkyWalking.Transport.Grpc.Common
{
    internal static class ExceptionHelpers
    {
        public static readonly string RegisterApplicationError = "Register application fail.";
        public static readonly string RegisterApplicationInstanceError = "Register application instance fail.";
        public static readonly string HeartbeatError = "Heartbeat fail.";
        public static readonly string CollectError = "Send trace segment fail.";

        public static readonly string RegisterServiceError = "Register service fail.";
        public static readonly string RegisterServiceInstanceError = "Register service instance fail.";
        public static readonly string PingError = "Ping server fail.";
    }
}