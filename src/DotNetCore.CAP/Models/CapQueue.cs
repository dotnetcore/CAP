// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Models
{
    public class CapQueue
    {
        public int MessageId { get; set; }

        /// <summary>
        /// 0 is CapSentMessage, 1 is CapReceivedMessage
        /// </summary>
        public MessageType MessageType { get; set; }
    }
}