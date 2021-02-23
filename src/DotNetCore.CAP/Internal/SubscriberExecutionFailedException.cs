// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Internal
{
    internal class SubscriberExecutionFailedException : Exception
    {
        private readonly Exception _originException;

        public SubscriberExecutionFailedException(string message, Exception ex) : base(message, ex)
        {
            _originException = ex;
        }

        public override string StackTrace => _originException.StackTrace;

        public override string Source => _originException.Source;
    }
}