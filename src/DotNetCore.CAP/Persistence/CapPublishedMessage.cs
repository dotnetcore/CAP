// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Messages
{
    public class CapPublishedMessage
    {
        public CapPublishedMessage()
        {
            Added = DateTime.Now;
        }

        public Message Message { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }

        public string StatusName { get; set; }
    }
}