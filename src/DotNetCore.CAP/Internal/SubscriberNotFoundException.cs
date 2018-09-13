﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Internal
{
    public class SubscriberNotFoundException : Exception
    {
        public SubscriberNotFoundException()
        {
        }

        public SubscriberNotFoundException(string message) : base(message)
        {
        }

        public SubscriberNotFoundException(string message, Exception inner) :
            base(message, inner)
        {
        }
    }
}