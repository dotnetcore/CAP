// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class InMemoryCapTransaction : CapTransactionBase
    {
        public InMemoryCapTransaction(IDispatcher dispatcher) : base(dispatcher)
        {
        }

        public override void Commit()
        { 
            Flush();
        }

        public override void Rollback()
        {
            //Ignore
        }

        public override void Dispose()
        {
        }
    }
}