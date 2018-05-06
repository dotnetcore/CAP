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

using System.Data.Common;
using Microsoft.Data.Sqlite;
using SkyWalking.NetworkProtocol.Trace;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public class SqliteEFCoreSpanMetadataProvider : IEfCoreSpanMetadataProvider
    {
        public IComponent Component { get; } = ComponentsDefine.EntityFrameworkCore_Sqlite;
        
        public bool Match(DbConnection connection)
        {
            return connection is SqliteConnection;
        }

        public string GetPeer(DbConnection connection)
        {
            string dataSource;
            switch (connection.DataSource)
            {
                    case "":
                        dataSource = "localhost";
                        break;
                    default:
                        dataSource = connection.DataSource;
                        break;
            }

            return $"{dataSource}";
        }
    }
}