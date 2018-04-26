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

using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace SkyWalking.NetworkProtocol.Trace
{
    public class ComponentsDefine
    {
        public static readonly OfficialComponent HttpClient = new OfficialComponent(2, "HttpClient");
        
        public static readonly OfficialComponent AspNetCore = new OfficialComponent(3001, "AspNetCore");

        public static readonly OfficialComponent EntityFrameworkCore = new OfficialComponent(3002, "EntityFrameworkCore");

        public static readonly OfficialComponent SqlClient = new OfficialComponent(3003, "SqlClient");
        
        public static readonly OfficialComponent CAP = new OfficialComponent(3004, "CAP");
        
        public static readonly OfficialComponent StackExchange_Redis = new OfficialComponent(3005, "StackExchange.Redis");
        
        public static readonly OfficialComponent SqlServer = new OfficialComponent(3006, "SqlServer");
        
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
            AddComponent(AspNetCore);
            AddComponent(EntityFrameworkCore);
            AddComponent(SqlClient);
            AddComponent(CAP);
            AddComponent(StackExchange_Redis);
            AddComponent(SqlServer);
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
