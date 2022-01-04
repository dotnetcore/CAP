// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using DotNetCore.CAP.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.OpenTelemetry
{
    public class CapDiagnosticProcessorObserver : IObserver<DiagnosticListener>
    {
        private readonly ILogger _logger;
        private readonly CapDiagnosticObserver _capObserver;
        public const string DiagnosticListenerName = CapDiagnosticListenerNames.DiagnosticListenerName;

        public CapDiagnosticProcessorObserver(ILogger<CapDiagnosticProcessorObserver> logger, CapDiagnosticObserver capObserver)
        {
            _logger = logger;
            _capObserver = capObserver;
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
                listener.Subscribe(_capObserver);
                _logger.LogInformation($"Loaded diagnostic listener [{DiagnosticListenerName}].");
            }
        }
    }
}