// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using DotNetCore.CAP.Diagnostics;

namespace DotNetCore.CAP.OpenTelemetry
{
    internal class DiagnosticSourceSubscriber : IDisposable, IObserver<System.Diagnostics.DiagnosticListener>
    {
        private readonly List<IDisposable> _listenerSubscriptions;
        private readonly Func<string, DiagnosticListener> _handlerFactory;
        private readonly Func<System.Diagnostics.DiagnosticListener, bool> _diagnosticSourceFilter;
        private readonly Func<string, object?, object?, bool>? _isEnabledFilter;
        private long _disposed;
        private IDisposable? _allSourcesSubscription;

        public DiagnosticSourceSubscriber(
            DiagnosticListener handler,
            Func<string, object?, object?, bool>? isEnabledFilter)
            : this(_ => handler, 
                value => CapDiagnosticListenerNames.DiagnosticListenerName == value.Name, 
                isEnabledFilter)
        {
        }

        public DiagnosticSourceSubscriber(
            Func<string, DiagnosticListener> handlerFactory,
            Func<System.Diagnostics.DiagnosticListener, bool> diagnosticSourceFilter,
            Func<string, object?, object?, bool>? isEnabledFilter)
        {
            _listenerSubscriptions = new List<IDisposable>();
            _handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
            _diagnosticSourceFilter = diagnosticSourceFilter;
            _isEnabledFilter = isEnabledFilter;
        }

        public void Subscribe()
        {
            _allSourcesSubscription ??= System.Diagnostics.DiagnosticListener.AllListeners.Subscribe(this);
        }

        public void OnNext(System.Diagnostics.DiagnosticListener value)
        {
            if ((Interlocked.Read(ref _disposed) == 0) && _diagnosticSourceFilter(value))
            {
                var handler = _handlerFactory(value.Name);
                var subscription = _isEnabledFilter == null ?
                    value.Subscribe(handler) :
                    value.Subscribe(handler, _isEnabledFilter);

                lock (_listenerSubscriptions)
                {
                    _listenerSubscriptions.Add(subscription);
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            lock (_listenerSubscriptions)
            {
                foreach (var listenerSubscription in _listenerSubscriptions)
                {
                    listenerSubscription?.Dispose();
                }

                _listenerSubscriptions.Clear();
            }

            _allSourcesSubscription?.Dispose();
            _allSourcesSubscription = null;
        }
    }
}