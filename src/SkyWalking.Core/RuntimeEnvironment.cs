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

namespace SkyWalking
{
    public class RuntimeEnvironment : IRuntimeEnvironment
    {
        public static IRuntimeEnvironment Instance { get; } = new RuntimeEnvironment();

        public NullableValue ServiceId { get; internal set; }

        public NullableValue ServiceInstanceId { get; internal set; }

        public bool Initialized => ServiceId.HasValue && ServiceInstanceId.HasValue;

        public Guid InstanceId { get; } = Guid.NewGuid();
        
        public IEnvironmentProvider Environment { get; set; }
    }
}