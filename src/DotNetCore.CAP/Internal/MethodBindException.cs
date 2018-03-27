// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Internal
{
    [Serializable]
    public class MethodBindException : Exception
    {
        public MethodBindException()
        {
        }

        public MethodBindException(string message) : base(message)
        {
        }

        public MethodBindException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}