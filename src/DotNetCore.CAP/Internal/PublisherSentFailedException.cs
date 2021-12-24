// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Internal
{
    public class PublisherSentFailedException : Exception
    {
        public PublisherSentFailedException(string message) : base(message)
        {
        }

        public PublisherSentFailedException(string message, Exception? ex) : base(message, ex)
        {
        }
    }
}