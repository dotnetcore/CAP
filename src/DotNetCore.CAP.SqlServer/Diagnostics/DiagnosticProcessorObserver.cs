// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;

namespace DotNetCore.CAP.SqlServer.Diagnostics
{
    public class DiagnosticProcessorObserver : IObserver<DiagnosticListener>
    {
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";
        private readonly IDispatcher _dispatcher;

        public DiagnosticProcessorObserver(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            BufferList = new ConcurrentDictionary<Guid, List<MediumMessage>>();
        }

        public ConcurrentDictionary<Guid, List<MediumMessage>> BufferList { get; }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == DiagnosticListenerName)
                listener.Subscribe(new DiagnosticObserver(_dispatcher, BufferList));
        }
    }
}