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
        
        public static readonly OfficialComponent Npgsql = new OfficialComponent(3007, "Npgsql");
        
        public static readonly OfficialComponent MySqlConnector = new OfficialComponent(3008, "MySqlConnector");
        
        public static readonly OfficialComponent EntityFrameworkCore_InMemory = new OfficialComponent(3009, "EntityFrameworkCore.InMemory");
        
        public static readonly OfficialComponent EntityFrameworkCore_SqlServer = new OfficialComponent(3010, "EntityFrameworkCore.SqlServer");
        
        public static readonly OfficialComponent EntityFrameworkCore_Sqlite = new OfficialComponent(3011, "EntityFrameworkCore.Sqlite");
        
        public static readonly OfficialComponent Pomelo_EntityFrameworkCore_MySql = new OfficialComponent(3012, "Pomelo.EntityFrameworkCore.MySql");
        
        public static readonly OfficialComponent Npgsql_EntityFrameworkCore_PostgreSQL = new OfficialComponent(3013, "Npgsql.EntityFrameworkCore.PostgreSQL");
        
        public static readonly OfficialComponent InMemoryDatabase = new OfficialComponent(3014, "InMemoryDatabase");
        
        public static readonly OfficialComponent AspNet = new OfficialComponent(3015, "AspNet");
        
        private static readonly ComponentsDefine _instance = new ComponentsDefine();

        public ComponentsDefine Instance => _instance;

        private readonly Dictionary<int, string> _components;

        private ComponentsDefine()
        {
            _components = new Dictionary<int, string>();
            AddComponent(AspNetCore);
            AddComponent(EntityFrameworkCore);
            AddComponent(SqlClient);
            AddComponent(CAP);
            AddComponent(StackExchange_Redis);
            AddComponent(SqlServer);
            AddComponent(Npgsql);
            AddComponent(MySqlConnector);
            AddComponent(EntityFrameworkCore_InMemory);
            AddComponent(EntityFrameworkCore_SqlServer);
            AddComponent(EntityFrameworkCore_Sqlite);
            AddComponent(Pomelo_EntityFrameworkCore_MySql);
            AddComponent(Npgsql_EntityFrameworkCore_PostgreSQL);
            AddComponent(InMemoryDatabase);
            AddComponent(AspNet);
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
