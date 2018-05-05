// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Models
{
    public abstract class CapMessage
    {
        public virtual string Id { get; set; }

        public virtual DateTime Timestamp { get; set; }

        public virtual string Content { get; set; }

        public virtual string CallbackName { get; set; }
    }

    public sealed class CapMessageDto : CapMessage
    {
        public CapMessageDto()
        {
            Id = ObjectId.GenerateNewStringId();
            Timestamp = DateTime.Now;
        }

        public CapMessageDto(string content) : this()
        {
            Content = content;
        }

        public override string Id { get; set; }

        public override DateTime Timestamp { get; set; }

        public override string Content { get; set; }

        public override string CallbackName { get; set; }
    }
}