using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SkyApm.Tracing;
using SmartSql;
using SmartSql.Diagnostics;

namespace SkyApm.Diagnostics.SmartSql
{
    public class SmartSqlTracingDiagnosticProcessor : ITracingDiagnosticProcessor
    {
        public string ListenerName => SmartSqlDiagnosticListenerExtensions.SMART_SQL_DIAGNOSTIC_LISTENER;

        private readonly ITracingContext _tracingContext;
        private readonly ILocalSegmentContextAccessor _localSegmentContextAccessor;

        public SmartSqlTracingDiagnosticProcessor(ITracingContext tracingContext,
            ILocalSegmentContextAccessor localSegmentContextAccessor)
        {
            _tracingContext = tracingContext;
            _localSegmentContextAccessor = localSegmentContextAccessor;
        }
        #region BeginTransaction
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_BEFORE_DB_SESSION_BEGINTRANSACTION)]
        public void BeforeDbSessionBeginTransaction([Object]DbSessionBeginTransactionBeforeEventData eventData)
        {
            var context = _tracingContext.CreateLocalSegmentContext("BeginTransaction");
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.DB;
            context.Span.Component = Common.Components.SMART_SQL;
            context.Span.AddTag(Common.Tags.DB_TYPE, "Sql");
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_AFTER_DB_SESSION_BEGINTRANSACTION)]
        public void AfterDbSessionBeginTransaction([Object]DbSessionBeginTransactionAfterEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.Peer = new Common.StringOrIntValue(eventData.DbSession.Connection?.DataSource);
                context.Span.AddTag(Common.Tags.DB_INSTANCE, eventData.DbSession.Connection?.Database);
                _tracingContext.Release(context);
            }
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_ERROR_DB_SESSION_BEGINTRANSACTION)]
        public void ErrorDbSessionBeginTransaction([Object]DbSessionBeginTransactionErrorEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(eventData.Exception);
                _tracingContext.Release(context);
            }
        }
        #endregion
        #region Commit
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_BEFORE_DB_SESSION_COMMIT)]
        public void BeforeDbSessionCommit([Object]DbSessionCommitBeforeEventData eventData)
        {
            var context = _tracingContext.CreateLocalSegmentContext(eventData.Operation);
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.DB;
            context.Span.Component = Common.Components.SMART_SQL;
            context.Span.Peer = new Common.StringOrIntValue(eventData.DbSession.Connection?.DataSource);
            context.Span.AddTag(Common.Tags.DB_INSTANCE, eventData.DbSession.Connection?.Database);
            context.Span.AddTag(Common.Tags.DB_TYPE, "Sql");
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_AFTER_DB_SESSION_COMMIT)]
        public void AfterDbSessionCommit([Object]DbSessionCommitAfterEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                _tracingContext.Release(context);
            }
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_ERROR_DB_SESSION_COMMIT)]
        public void ErrorDbSessionCommit([Object]DbSessionCommitErrorEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(eventData.Exception);
                _tracingContext.Release(context);
            }
        }
        #endregion
        #region Rollback
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_BEFORE_DB_SESSION_ROLLBACK)]
        public void BeforeDbSessionRollback([Object]DbSessionRollbackBeforeEventData eventData)
        {
            var context = _tracingContext.CreateLocalSegmentContext(eventData.Operation);
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.DB;
            context.Span.Component = Common.Components.SMART_SQL;
            context.Span.Peer = new Common.StringOrIntValue(eventData.DbSession.Connection?.DataSource);
            context.Span.AddTag(Common.Tags.DB_INSTANCE, eventData.DbSession.Connection?.Database);
            context.Span.AddTag(Common.Tags.DB_TYPE, "Sql");
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_AFTER_DB_SESSION_ROLLBACK)]
        public void AfterDbSessionRollback([Object]DbSessionRollbackAfterEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                _tracingContext.Release(context);
            }
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_ERROR_DB_SESSION_ROLLBACK)]
        public void ErrorDbSessionRollback([Object]DbSessionRollbackErrorEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(eventData.Exception);
                _tracingContext.Release(context);
            }
        }
        #endregion
        #region Dispose
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_BEFORE_DB_SESSION_DISPOSE)]
        public void BeforeDbSessionDispose([Object]DbSessionDisposeBeforeEventData eventData)
        {
            var context = _tracingContext.CreateLocalSegmentContext(eventData.Operation);
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.DB;
            context.Span.Component = Common.Components.SMART_SQL;
            context.Span.Peer = new Common.StringOrIntValue(eventData.DbSession.Connection?.DataSource);
            context.Span.AddTag(Common.Tags.DB_INSTANCE, eventData.DbSession.Connection?.Database);
            context.Span.AddTag(Common.Tags.DB_TYPE, "Sql");
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_AFTER_DB_SESSION_DISPOSE)]
        public void AfterDbSessionDispose([Object]DbSessionDisposeAfterEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                _tracingContext.Release(context);
            }
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_ERROR_DB_SESSION_DISPOSE)]
        public void ErrorDbSessionDispose([Object]DbSessionDisposeErrorEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(eventData.Exception);
                _tracingContext.Release(context);
            }
        }
        #endregion
        #region Open
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_BEFORE_DB_SESSION_OPEN)]
        public void BeforeDbSessionOpen([Object]DbSessionOpenBeforeEventData eventData)
        {
            var context = _tracingContext.CreateLocalSegmentContext(eventData.Operation);
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.DB;
            context.Span.Component = Common.Components.SMART_SQL;
            context.Span.AddTag(Common.Tags.DB_TYPE, "Sql");
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_AFTER_DB_SESSION_OPEN)]
        public void AfterDbSessionOpen([Object]DbSessionOpenAfterEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.Peer = new Common.StringOrIntValue(eventData.DbSession.Connection?.DataSource);
                context.Span.AddTag(Common.Tags.DB_INSTANCE, eventData.DbSession.Connection?.Database);
                _tracingContext.Release(context);
            }
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_ERROR_DB_SESSION_OPEN)]
        public void ErrorDbSessionOpen([Object]DbSessionOpenErrorEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(eventData.Exception);
                _tracingContext.Release(context);
            }
        }
        #endregion
        #region Invoke
        private static string ResolveOperationName(ExecutionContext executionContext)
        {
            return executionContext.Request.FullSqlId != "." ?
                executionContext.Request.FullSqlId : executionContext.Request.ExecutionType.ToString();
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_BEFORE_DB_SESSION_INVOKE)]
        public void BeforeDbSessionInvoke([Object]DbSessionInvokeBeforeEventData eventData)
        {
            var context = _tracingContext.CreateLocalSegmentContext(ResolveOperationName(eventData.ExecutionContext));
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.DB;
            context.Span.Component = Common.Components.SMART_SQL;
            context.Span.AddTag(Common.Tags.DB_TYPE, "Sql");
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_AFTER_DB_SESSION_INVOKE)]
        public void AfterDbSessionInvoke([Object]DbSessionInvokeAfterEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.AddTag("from_cache", eventData.ExecutionContext.Result.FromCache);
                var resultSize = eventData.ExecutionContext.Result.IsList
                    ? (eventData.ExecutionContext.Result.GetData() as ICollection)?.Count
                    : 1;
                context.Span.AddTag("result_size", resultSize?.ToString());
                _tracingContext.Release(context);
            }
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_ERROR_DB_SESSION_INVOKE)]
        public void ErrorDbSessionInvoke([Object]DbSessionInvokeErrorEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(eventData.Exception);
                _tracingContext.Release(context);
            }
        }
        #endregion

        #region CommandExecuter

        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_BEFORE_COMMAND_EXECUTER_EXECUTE)]
        public void BeforeCommandExecuterExecute([Object]CommandExecuterExecuteBeforeEventData eventData)
        {
            var context = _tracingContext.CreateLocalSegmentContext(eventData.Operation);
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.DB;
            context.Span.Component = Common.Components.SMART_SQL;
            context.Span.AddTag(Common.Tags.DB_TYPE, "Sql");
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_AFTER_COMMAND_EXECUTER_EXECUTE)]
        public void AfterCommandExecuterExecute([Object]CommandExecuterExecuteAfterEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.Peer = new Common.StringOrIntValue(eventData.ExecutionContext.DbSession.Connection?.DataSource);
                context.Span.AddTag(Common.Tags.DB_INSTANCE, eventData.ExecutionContext.DbSession.Connection?.Database);
                context.Span.AddTag(Common.Tags.DB_STATEMENT, eventData.ExecutionContext.Request.RealSql);
                _tracingContext.Release(context);
            }
        }
        [DiagnosticName(SmartSqlDiagnosticListenerExtensions.SMART_SQL_ERROR_COMMAND_EXECUTER_EXECUTE)]
        public void ErrorCommandExecuterExecute([Object]CommandExecuterExecuteErrorEventData eventData)
        {
            var context = _localSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(eventData.Exception);
                _tracingContext.Release(context);
            }
        }

        #endregion
    }
}
