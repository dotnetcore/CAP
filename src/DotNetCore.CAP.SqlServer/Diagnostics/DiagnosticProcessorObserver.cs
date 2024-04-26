// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace DotNetCore.CAP.SqlServer.Diagnostics;

public class DiagnosticProcessorObserver : IObserver<DiagnosticListener>
{
    public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

    public DiagnosticProcessorObserver()
    {
        TransBuffer = new ConcurrentDictionary<Guid, SqlServerCapTransaction>();
    }

    public ConcurrentDictionary<Guid, SqlServerCapTransaction> TransBuffer { get; }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == DiagnosticListenerName)
            listener.Subscribe(new DiagnosticObserver(TransBuffer));
    }
}