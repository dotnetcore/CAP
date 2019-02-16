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

namespace SkyApm.Common
{
    public static class Components
    {
        public static readonly StringOrIntValue ASPNETCORE= new StringOrIntValue("AspNetCore");

        public static readonly StringOrIntValue HTTPCLIENT = new StringOrIntValue("HttpClient");
        
        public static readonly StringOrIntValue ENTITYFRAMEWORKCORE = new StringOrIntValue("EntityFrameworkCore");
        
        public static readonly StringOrIntValue SQLCLIENT = new StringOrIntValue("SqlClient");
        
        public static readonly StringOrIntValue CAP = new StringOrIntValue("CAP");
        
        public static readonly StringOrIntValue ENTITYFRAMEWORKCORE_SQLITE = new StringOrIntValue("EntityFrameworkCore.Sqlite");
        
        public static readonly StringOrIntValue POMELO_ENTITYFRAMEWORKCORE_MYSQL = new StringOrIntValue("Pomelo.EntityFrameworkCore.MySql");
        
        public static readonly StringOrIntValue NPGSQL_ENTITYFRAMEWORKCORE_POSTGRESQL = new StringOrIntValue("Npgsql.EntityFrameworkCore.PostgreSQL");
        
        public static readonly StringOrIntValue ASPNET = new StringOrIntValue("AspNet");
    }
}