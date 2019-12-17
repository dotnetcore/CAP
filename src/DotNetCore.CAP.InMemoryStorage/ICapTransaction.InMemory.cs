// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Transport;

namespace DotNetCore.CAP.InMemoryStorage
{
    internal class InMemoryCapTransaction : CapTransactionBase
    {
        public InMemoryCapTransaction(IDispatcher dispatcher) : base(dispatcher)
        {
        }

        public override void Commit()
        { 
            Flush();
        }

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public override void Rollback()
        {
            //Ignore
        }

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
        }
    }
}