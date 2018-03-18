/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
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

using System.Collections.Generic;

namespace SkyWalking.NetworkProtocol.Trace
{
    public class ComponentsDefine
    {
        public static readonly OfficialComponent AspNetCore = new OfficialComponent(1, "AspNetCore");

        private static readonly ComponentsDefine _instance = new ComponentsDefine();

        public ComponentsDefine Instance
        {
            get
            {
                return _instance;
            }
        }

        private Dictionary<int, string> _components;

        private ComponentsDefine()
        {
            _components = new Dictionary<int, string>();
        }

        private void AddComponent(OfficialComponent component)
        {
            _components[component.Id] = component.Name;
        }

        public string GetComponentName(int componentId)
        {
            if (_components.TryGetValue(componentId, out var value))
            {
                return value;
            }
            return null;
        }
    }
}
