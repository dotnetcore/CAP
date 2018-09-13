// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.SqlServer.Diagnostics
{
    internal class DiagnosticObserver : IObserver<KeyValuePair<string, object>>
    {
        private const string SqlClientPrefix = "System.Data.SqlClient.";

        public const string SqlAfterCommitTransaction = SqlClientPrefix + "WriteTransactionCommitAfter";
        public const string SqlErrorCommitTransaction = SqlClientPrefix + "WriteTransactionCommitError";
        private readonly ConcurrentDictionary<Guid, List<CapPublishedMessage>> _bufferList;
        private readonly IDispatcher _dispatcher;

        public DiagnosticObserver(IDispatcher dispatcher,
            ConcurrentDictionary<Guid, List<CapPublishedMessage>> bufferList)
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
            if (evt.Key == SqlAfterCommitTransaction)
            {
                var sqlConnection = (SqlConnection) GetProperty(evt.Value, "Connection");
                var transactionKey = sqlConnection.ClientConnectionId;
                if (_bufferList.TryRemove(transactionKey, out var msgList))
                {
                    foreach (var message in msgList)
                    {
                        _dispatcher.EnqueueToPublish(message);
                    }
                }
            }
            else if (evt.Key == SqlErrorCommitTransaction)
            {
                var sqlConnection = (SqlConnection) GetProperty(evt.Value, "Connection");
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