// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class MessageQueryDto
    {
        public MessageType MessageType { get; set; }

        public string Group { get; set; }
        public string Name { get; set; }

        public string Content { get; set; }

        public string StatusName { get; set; }

        public int CurrentPage { get; set; }

        public int PageSize { get; set; }
    }
}