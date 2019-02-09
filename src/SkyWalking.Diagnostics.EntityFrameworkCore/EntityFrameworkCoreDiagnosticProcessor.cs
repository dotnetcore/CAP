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
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public class EntityFrameworkCoreTracingDiagnosticProcessor : ITracingDiagnosticProcessor
    {
        private const string TRACE_ORM = "TRACE_ORM";
        private Func<CommandEventData, string> _operationNameResolver;
        private readonly IEfCoreSpanFactory _efCoreSpanFactory;

        public string ListenerName => DbLoggerCategory.Name;

        /// <summary>
        /// A delegate that returns the OpenTracing "operation name" for the given command.
        /// </summary>
        public Func<CommandEventData, string> OperationNameResolver
        {
            get
            {
                return _operationNameResolver ??
                       (_operationNameResolver = (data) =>
                       {
                           var commandType = data.Command.CommandText?.Split(' ');
                           return "DB " + (commandType.FirstOrDefault() ?? data.ExecuteMethod.ToString());
                       });
            }
            set => _operationNameResolver = value ?? throw new ArgumentNullException(nameof(OperationNameResolver));
        }

        public EntityFrameworkCoreTracingDiagnosticProcessor(IEfCoreSpanFactory spanFactory)
        {
            _efCoreSpanFactory = spanFactory;
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting")]
        public void CommandExecuting([Object] CommandEventData eventData)
        {
            var operationName = OperationNameResolver(eventData);
            var span = _efCoreSpanFactory.Create(operationName, eventData);
            span.SetLayer(SpanLayer.DB);
            Tags.DbType.Set(span, "Sql");
            Tags.DbInstance.Set(span, eventData.Command.Connection.Database);
            Tags.DbStatement.Set(span, eventData.Command.CommandText);
            Tags.DbBindVariables.Set(span, BuildParameterVariables(eventData.Command.Parameters));
            ContextManager.ContextProperties[TRACE_ORM] = true;
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted")]
        public void CommandExecuted()
        {
            ContextManager.StopSpan();
            ContextManager.ContextProperties.Remove(TRACE_ORM);
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.Database.Command.CommandError")]
        public void CommandError([Object]CommandErrorEventData eventData)
        {
            var span = ContextManager.ActiveSpan;
            if (span == null)
            {
                return;
            }

            if (eventData != null)
            {
                span.Log(eventData.Exception);
            }
            span.ErrorOccurred();
            ContextManager.StopSpan(span);
            ContextManager.ContextProperties.Remove(TRACE_ORM);
        }

        private string BuildParameterVariables(DbParameterCollection dbParameters)
        {
            if (dbParameters == null)
            {
                return string.Empty;
            }

            return dbParameters.FormatParameters(false);
        }
    }
}