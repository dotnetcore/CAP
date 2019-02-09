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


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MySql.Data.MySqlClient;
using SkyWalking.Components;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;

namespace SkyWalking.Diagnostics.MySqlData
{
    public class MySqlDataDiagnosticProcessor : DefaultTraceListener, ITracingDiagnosticProcessor
    {
        private Dictionary<long, MySqlConnectionStringBuilder> _dbConn =
            new Dictionary<long, MySqlConnectionStringBuilder>();
        public MySqlDataDiagnosticProcessor()
        {
            MySqlTrace.Listeners.Clear();
            MySqlTrace.Listeners.Add(this);
            MySqlTrace.Switch.Level = SourceLevels.Information;
            MySqlTrace.QueryAnalysisEnabled = true;
        }

        private static string ResolveOperationName(MySqlDataTraceCommand sqlCommand)
        {
            var commandType = sqlCommand.SqlText?.Split(' ');
            return $"{MySqlDataDiagnosticStrings.MySqlDataPrefix}{commandType?.FirstOrDefault()}";
        }

        public string ListenerName { get; } = MySqlDataDiagnosticStrings.DiagnosticListenerName;

        public void BeforeExecuteCommand(MySqlDataTraceCommand sqlCommand)
        {
            var peer = sqlCommand.DbServer;
            var span = ContextManager.CreateExitSpan(ResolveOperationName(sqlCommand), peer);
            span.SetLayer(SpanLayer.DB);
            span.SetComponent(ComponentsDefine.MySqlData);
            Tags.DbType.Set(span, "MySql");
            Tags.DbInstance.Set(span, sqlCommand.Database);
            Tags.DbStatement.Set(span, sqlCommand.SqlText);
        }

        public void AfterExecuteCommand()
        {
            ContextManager.StopSpan();
        }

        public void ErrorExecuteCommand(Exception ex)
        {
            var span = ContextManager.ActiveSpan;
            span?.ErrorOccurred();
            span?.Log(ex);
            span?.Log(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new Dictionary<string, object>()
            {
                { "event", "error"},
                { "error.kind", "MySqlException"},
                { "message", ex.Message}
            });
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventCache"></param>
        /// <param name="source"></param>
        /// <param name="eventType"></param>
        /// <param name="id"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string format, params object[] args)
        {
            switch ((MySqlTraceEventType)id)
            {
                case MySqlTraceEventType.ConnectionOpened:
                    var driverId = (long)args[0];
                    var connStr = args[1].ToString();
                    _dbConn[driverId] = new MySqlConnectionStringBuilder(connStr);
                    break;
                case MySqlTraceEventType.ConnectionClosed:
                    //TODO
                    break;
                case MySqlTraceEventType.QueryOpened:
                    BeforeExecuteCommand(GetCommand(args[0], args[2]));
                    break;
                case MySqlTraceEventType.ResultOpened:
                    //TODO
                    break;
                case MySqlTraceEventType.ResultClosed:
                    //TODO
                    break;
                case MySqlTraceEventType.QueryClosed:
                    AfterExecuteCommand();
                    break;
                case MySqlTraceEventType.StatementPrepared:
                    //TODO
                    break;
                case MySqlTraceEventType.StatementExecuted:
                    //TODO
                    break;
                case MySqlTraceEventType.StatementClosed:
                    //TODO
                    break;
                case MySqlTraceEventType.NonQuery:
                    //TODO
                    break;
                case MySqlTraceEventType.UsageAdvisorWarning:
                    //TODO
                    break;
                case MySqlTraceEventType.Warning:
                    //TODO
                    break;
                case MySqlTraceEventType.Error:
                    ErrorExecuteCommand(GetMySqlErrorException(args[1], args[2]));
                    break;
                case MySqlTraceEventType.QueryNormalized:
                    //TODO
                    break;
            }
        }


        private MySqlDataTraceCommand GetCommand(object driverIdObj, object cmd)
        {
            var command = new MySqlDataTraceCommand();
            if (_dbConn.TryGetValue((long)driverIdObj, out var database))
            {
                command.Database = database.Database;
                command.DbServer = database.Server;
            }

            command.SqlText = (cmd == null ? "" : cmd.ToString());
            return command;
        }

        private Exception GetMySqlErrorException(object errorCode, object errorMsg)
        {
            //TODO handle errorcode
            return new Exception($"{errorMsg}");
        }
    }
}
