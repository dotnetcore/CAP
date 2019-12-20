// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    internal class CapMqSender
    {
        private readonly IDispatcher _dispatcher;
        private readonly ConcurrentQueue<MediumMessage> _bufferList;

        public CapMqSender(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _bufferList = new ConcurrentQueue<MediumMessage>();
        }

        protected internal virtual void AddToSent(MediumMessage msg)
        {
            _bufferList.Enqueue(msg);
        }

        protected internal virtual void Flush()
        {
            while (!_bufferList.IsEmpty)
            {
                _bufferList.TryDequeue(out var message);

                _dispatcher.EnqueueToPublish(message);
            }
        }
    }
}