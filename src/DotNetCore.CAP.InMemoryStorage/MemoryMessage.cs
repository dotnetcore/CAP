// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.InMemoryStorage
{
    internal class MemoryMessage : MediumMessage
    {
        public string Name { get; set; }

        public StatusName StatusName { get; set; }

        public string Group { get; set; }
    }
}
