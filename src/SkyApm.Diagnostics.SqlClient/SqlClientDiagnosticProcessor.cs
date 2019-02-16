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

using System;
using System.Data.SqlClient;
using System.Linq;
using SkyApm.Tracing;

namespace SkyApm.Diagnostics.SqlClient
{
    public class SqlClientTracingDiagnosticProcessor : ITracingDiagnosticProcessor
    {
        private readonly ITracingContext _tracingContext;
        private readonly IExitSegmentContextAccessor _contextAccessor;

        public SqlClientTracingDiagnosticProcessor(ITracingContext tracingContext,
            IExitSegmentContextAccessor contextAccessor)
        {
            _tracingContext = tracingContext;
            _contextAccessor = contextAccessor;
        }
        
        public string ListenerName { get; } = SqlClientDiagnosticStrings.DiagnosticListenerName;

        private static string ResolveOperationName(SqlCommand sqlCommand)
        {
            var commandType = sqlCommand.CommandText?.Split(' ');
            return $"{SqlClientDiagnosticStrings.SqlClientPrefix}{commandType?.FirstOrDefault()}";
        }
        
        [DiagnosticName(SqlClientDiagnosticStrings.SqlBeforeExecuteCommand)]
        public void BeforeExecuteCommand([Property(Name = "Command")] SqlCommand sqlCommand)
        {
            var context = _tracingContext.CreateExitSegmentContext(ResolveOperationName(sqlCommand),
                sqlCommand.Connection.DataSource);
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.DB;
            context.Span.Component = Common.Components.SQLCLIENT;
            context.Span.AddTag(Common.Tags.DB_TYPE, "sql");
            context.Span.AddTag(Common.Tags.DB_INSTANCE, sqlCommand.Connection.Database);
            context.Span.AddTag(Common.Tags.DB_STATEMENT,  sqlCommand.CommandText);
        }

        [DiagnosticName(SqlClientDiagnosticStrings.SqlAfterExecuteCommand)]
        public void AfterExecuteCommand()
        {
            var context = _contextAccessor.Context;
            if (context != null)
            {
                _tracingContext.Release(context);
            }   
        }

        [DiagnosticName(SqlClientDiagnosticStrings.SqlErrorExecuteCommand)]
        public void ErrorExecuteCommand([Property(Name = "Exception")] Exception ex)
        {
            var context = _contextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(ex);
                _tracingContext.Release(context);
            }   
        }
    }
}