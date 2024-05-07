// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DotNetCore.CAP.Internal;
using Microsoft.Data.SqlClient;

namespace DotNetCore.CAP.SqlServer.Diagnostics;

internal class DiagnosticObserver : IObserver<KeyValuePair<string, object?>>
{
    public const string SqlAfterCommitTransactionMicrosoft = "Microsoft.Data.SqlClient.WriteTransactionCommitAfter";
    public const string SqlErrorCommitTransactionMicrosoft = "Microsoft.Data.SqlClient.WriteTransactionCommitError";
    public const string SqlAfterRollbackTransactionMicrosoft = "Microsoft.Data.SqlClient.WriteTransactionRollbackAfter";
    public const string SqlBeforeCloseConnectionMicrosoft = "Microsoft.Data.SqlClient.WriteConnectionCloseBefore";

    private readonly ConcurrentDictionary<Guid, SqlServerCapTransaction> _transBuffer;

    public DiagnosticObserver(ConcurrentDictionary<Guid, SqlServerCapTransaction> bufferTrans)
    {
        _transBuffer = bufferTrans;
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
                    if (!TryGetSqlConnection(evt, out var sqlConnection)) return;
                    var transactionKey = sqlConnection.ClientConnectionId;

                    if (_transBuffer.TryRemove(transactionKey, out var transaction))
                    {
                        if (GetProperty(evt.Value, "Operation") as string == "Rollback")
                        {
                            transaction.Dispose();
                            return;
                        }

                        transaction.DbTransaction = new NoopTransaction();
                        transaction.Commit();
                        transaction.Dispose();
                    }

                    break;
                }
            case SqlErrorCommitTransactionMicrosoft or SqlAfterRollbackTransactionMicrosoft
                or SqlBeforeCloseConnectionMicrosoft:
                {
                    if (!_transBuffer.IsEmpty)
                    {
                        if (!TryGetSqlConnection(evt, out var sqlConnection)) return;
                        var transactionKey = sqlConnection.ClientConnectionId;

                        if (_transBuffer.TryRemove(transactionKey, out var transaction))
                        {
                            transaction.Dispose();
                        }
                    }

                    break;
                }
        }
    }

    private static bool TryGetSqlConnection(KeyValuePair<string, object?> evt,
        [NotNullWhen(true)] out SqlConnection? sqlConnection)
    {
        sqlConnection = GetProperty(evt.Value, "Connection") as SqlConnection;
        return sqlConnection != null;
    }

    private static object? GetProperty(object? @this, string propertyName)
    {
        return @this?.GetType().GetTypeInfo().GetDeclaredProperty(propertyName)?.GetValue(@this);
    }
}