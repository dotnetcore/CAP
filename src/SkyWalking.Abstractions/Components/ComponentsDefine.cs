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

using System.Collections.Generic;

namespace SkyWalking.Components
{
    public class ComponentsDefine
    {
        public static readonly IComponent HttpClient = new OfficialComponent(2, "HttpClient");
        
        public static readonly IComponent AspNetCore = new OfficialComponent(3001, "AspNetCore");

        public static readonly IComponent EntityFrameworkCore = new OfficialComponent(3002, "EntityFrameworkCore");

        public static readonly IComponent SqlClient = new OfficialComponent(3003, "SqlClient");
        
        public static readonly IComponent CAP = new OfficialComponent(3004, "CAP");
        
        public static readonly IComponent StackExchange_Redis = new OfficialComponent(3005, "StackExchange.Redis");
        
        public static readonly IComponent SqlServer = new OfficialComponent(3006, "SqlServer");
        
        public static readonly IComponent Npgsql = new OfficialComponent(3007, "Npgsql");
        
        public static readonly IComponent MySqlConnector = new OfficialComponent(3008, "MySqlConnector");
        
        public static readonly IComponent EntityFrameworkCore_InMemory = new OfficialComponent(3009, "EntityFrameworkCore.InMemory");
        
        public static readonly IComponent EntityFrameworkCore_SqlServer = new OfficialComponent(3010, "EntityFrameworkCore.SqlServer");
        
        public static readonly IComponent EntityFrameworkCore_Sqlite = new OfficialComponent(3011, "EntityFrameworkCore.Sqlite");
        
        public static readonly IComponent Pomelo_EntityFrameworkCore_MySql = new OfficialComponent(3012, "Pomelo.EntityFrameworkCore.MySql");
        
        public static readonly IComponent Npgsql_EntityFrameworkCore_PostgreSQL = new OfficialComponent(3013, "Npgsql.EntityFrameworkCore.PostgreSQL");
        
        public static readonly IComponent InMemoryDatabase = new OfficialComponent(3014, "InMemoryDatabase");
        
        public static readonly IComponent AspNet = new OfficialComponent(3015, "AspNet");

        public static readonly IComponent MySqlData = new OfficialComponent(3016, "MySql.Data");
    }
}
