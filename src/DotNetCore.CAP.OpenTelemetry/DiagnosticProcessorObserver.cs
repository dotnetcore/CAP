// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using DotNetCore.CAP.Diagnostics;

namespace DotNetCore.CAP.OpenTelemetry
{
    public class CapDiagnosticProcessorObserver : IObserver<DiagnosticListener>
    {
        public const string DiagnosticListenerName = CapDiagnosticListenerNames.DiagnosticListenerName;

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
                listener.Subscribe(new CapDiagnosticObserver());
            }
        }
    }
}