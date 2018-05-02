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

namespace SkyWalking.Diagnostics.SqlClient
{
    internal static class SqlClientDiagnosticStrings
    {
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

        public const string SqlClientPrefix = "sqlClient ";

        public const string SqlBeforeExecuteCommand = "System.Data.SqlClient.WriteCommandBefore";
        public const string SqlAfterExecuteCommand = "System.Data.SqlClient.WriteCommandAfter";
        public const string SqlErrorExecuteCommand = "System.Data.SqlClient.WriteCommandError";
    }
}