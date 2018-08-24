using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.SqlServer.Diagnostics
{
    public class DiagnosticProcessorObserver : IObserver<DiagnosticListener>
    {
        private readonly IDispatcher _dispatcher;
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

        public ConcurrentDictionary<Guid, List<CapPublishedMessage>> BufferList { get; }

        public DiagnosticProcessorObserver(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            BufferList = new ConcurrentDictionary<Guid, List<CapPublishedMessage>>();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == DiagnosticListenerName)
            {
                listener.Subscribe(new DiagnosticObserver(_dispatcher, BufferList));
            }
        }
    }
}