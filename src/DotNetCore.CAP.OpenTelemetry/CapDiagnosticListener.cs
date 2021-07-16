using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.Tracing;
using OpenTelemetry.Instrumentation;
using System.Diagnostics;
using System.Reflection;

namespace DotNetCore.CAP.OpenTelemetry
{
    public class CapDiagnosticListener : EventSource
    {
        internal static readonly ActivitySource ActivitySource = new ActivitySource("", "");

        public CapDiagnosticListener()
        {
            // var count = new System.Diagnostics.Tracing.DiagnosticCounter();

        }

    }

    internal sealed partial class HttpTelemetry : EventSource
    {
        public static readonly HttpTelemetry Log = new HttpTelemetry();


        [Event(1, Level = EventLevel.Informational)]
        private void RequestStart(string scheme, string host, int port, string pathAndQuery, byte versionMajor, byte versionMinor, HttpVersionPolicy versionPolicy)
        {
            Interlocked.Increment(ref _startedRequests);
            WriteEvent(eventId: 1, scheme, host, port, pathAndQuery, versionMajor, versionMinor, versionPolicy);
        }
    }
}
