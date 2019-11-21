// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Data.SqlClient;

namespace DotNetCore.CAP.SqlServer.Diagnostics
{
    internal class DiagnosticObserver : IObserver<KeyValuePair<string, object>>
    {
        public const string SqlAfterCommitTransaction = "System.Data.SqlClient.WriteTransactionCommitAfter";
        public const string SqlAfterCommitTransactionMicrosoft = "Microsoft.Data.SqlClient.WriteTransactionCommitAfter";
        public const string SqlErrorCommitTransaction = "System.Data.SqlClient.WriteTransactionCommitError";
        public const string SqlErrorCommitTransactionMicrosoft = "Microsoft.Data.SqlClient.WriteTransactionCommitError";

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

        public void OnNext(KeyValuePair<string, object> evt)
        {
            if (evt.Key == SqlAfterCommitTransaction || evt.Key == SqlAfterCommitTransactionMicrosoft)
            {
                var sqlConnection = (SqlConnection)GetProperty(evt.Value, "Connection");
                var transactionKey = sqlConnection.ClientConnectionId;
                if (_bufferList.TryRemove(transactionKey, out var msgList))
                    foreach (var message in msgList)
                    {
                        _dispatcher.EnqueueToPublish(message);
                    }
            }
            else if (evt.Key == SqlErrorCommitTransaction || evt.Key == SqlErrorCommitTransactionMicrosoft)
            {
                var sqlConnection = (SqlConnection)GetProperty(evt.Value, "Connection");
                var transactionKey = sqlConnection.ClientConnectionId;

                _bufferList.TryRemove(transactionKey, out _);
            }
        }

        private static object GetProperty(object _this, string propertyName)
        {
            return _this.GetType().GetTypeInfo().GetDeclaredProperty(propertyName)?.GetValue(_this);
        }
    }
}