// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.OpenTelemetry
{
    /// <summary>
    /// CAP instrumentation.
    /// </summary>
    internal class CapInstrumentation : IDisposable
    {
        private readonly DiagnosticSourceSubscriber? _diagnosticSourceSubscriber;

        public CapInstrumentation(DiagnosticListener diagnosticListener)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(diagnosticListener, null);
            _diagnosticSourceSubscriber.Subscribe();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _diagnosticSourceSubscriber?.Dispose();
        }
    }
}
