// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Data;

namespace DotNetCore.CAP.Internal
{
    internal class RelationDbTransaction : IDbTransaction
    {
        private readonly ICapTransaction _capTransaction;

        public RelationDbTransaction(ICapTransaction capTransaction)
        {
            _capTransaction = capTransaction;
            var dbTransaction = (IDbTransaction) capTransaction.DbTransaction;
            Connection = dbTransaction.Connection;
            IsolationLevel = dbTransaction.IsolationLevel;
        }

        public void Dispose()
        {
            _capTransaction.Dispose();
        }

        public void Commit()
        {
            _capTransaction.Commit();
        }

        public void Rollback()
        {
            _capTransaction.Rollback();
        }

        public IDbConnection Connection { get; }
        public IsolationLevel IsolationLevel { get; }
    }
}