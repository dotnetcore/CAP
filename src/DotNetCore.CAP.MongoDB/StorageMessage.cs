// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.MongoDB
{
    internal class ReceivedMessage
    {
        public long Id { get; set; }

        public string Version { get; set; } = default!;

        public string Group { get; set; } = default!;

        public string Name { get; set; } = default!;

        public string Content { get; set; } = default!;

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }

        public string StatusName { get; set; } = default!;
    }

    internal class PublishedMessage
    {
        public long Id { get; set; }

        public string Version { get; set; } = default!;

        public string Name { get; set; } = default!;

        public string Content { get; set; } = default!;

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }

        public string StatusName { get; set; } = default!;
    }
}