// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Data.SqlClient;

namespace DotNetCore.CAP.SqlServer.Diagnostics
{
    internal class DiagnosticObserver : IObserver<KeyValuePair<string, object?>>
    {
        public const string SqlAfterCommitTransactionMicrosoft = "Microsoft.Data.SqlClient.WriteTransactionCommitAfter";
        public const string SqlErrorCommitTransactionMicrosoft = "Microsoft.Data.SqlClient.WriteTransactionCommitError";
        public const string SqlAfterRollbackTransactionMicrosoft = "Microsoft.Data.SqlClient.WriteTransactionRollbackAfter";
        public const string SqlBeforeCloseConnectionMicrosoft = "Microsoft.Data.SqlClient.WriteConnectionCloseBefore";

        private readonly ConcurrentDictionary<Guid, List<MediumMessage>> _bufferList;
        private readonly IDispatcher _dispatcher;

        public DiagnosticObserver(IDispatcher dispatcher,
            ConcurrentDictionary<Guid, List<MediumMessage>> bufferList)
        {
            _dispatcher = dispatcher;
            _bufferList = bufferList;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> evt)
        {
            switch (evt.Key)
            {
                case SqlAfterCommitTransactionMicrosoft:
                {
                    if (!TryGetSqlConnection(evt, out SqlConnection? sqlConnection)) return;
                    var transactionKey = sqlConnection.ClientConnectionId;
                    if (_bufferList.TryRemove(transactionKey, out var msgList))
                    {
                        foreach (var message in msgList)
                        {
                            _dispatcher.EnqueueToPublish(message);
                        }
                    }

                    break;
                }
                case SqlErrorCommitTransactionMicrosoft or SqlAfterRollbackTransactionMicrosoft or SqlBeforeCloseConnectionMicrosoft:
                {
                    if (!_bufferList.IsEmpty)
                    {
                        if (!TryGetSqlConnection(evt, out SqlConnection? sqlConnection)) return;
                        var transactionKey = sqlConnection.ClientConnectionId;

                        _bufferList.TryRemove(transactionKey, out _);
                    }

                    break;
                }
            }
        }

        private static bool TryGetSqlConnection(KeyValuePair<string, object?> evt, [NotNullWhen(true)] out SqlConnection? sqlConnection)
        {
            sqlConnection = GetProperty(evt.Value, "Connection") as SqlConnection;
            return sqlConnection != null;
        }

        private static object? GetProperty(object? @this, string propertyName)
        {
            return @this?.GetType().GetTypeInfo().GetDeclaredProperty(propertyName)?.GetValue(@this);
        }
    }
}