/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The OpenSkywalking licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SkyWalking.Logging;
using SkyWalking.Utils;

namespace SkyWalking.Diagnostics
{
    public class TracingDiagnosticProcessorObserver : IObserver<DiagnosticListener>
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IEnumerable<ITracingDiagnosticProcessor> _tracingDiagnosticProcessors;

        public TracingDiagnosticProcessorObserver(IEnumerable<ITracingDiagnosticProcessor> tracingDiagnosticProcessors,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(TracingDiagnosticProcessorObserver));
            _loggerFactory = loggerFactory;
            _tracingDiagnosticProcessors = tracingDiagnosticProcessors ??
                                           throw new ArgumentNullException(nameof(tracingDiagnosticProcessors));
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener listener)
        {
            foreach (var diagnosticProcessor in _tracingDiagnosticProcessors.Distinct(x => x.ListenerName))
            {
                if (listener.Name == diagnosticProcessor.ListenerName)
                {
                    Subscribe(listener, diagnosticProcessor);
                    _logger.Information(
                        $"Loaded diagnostic listener [{diagnosticProcessor.ListenerName}].");
                }
            }
        }

        protected virtual void Subscribe(DiagnosticListener listener,
            ITracingDiagnosticProcessor tracingDiagnosticProcessor)
        {
            listener.Subscribe(new TracingDiagnosticObserver(tracingDiagnosticProcessor, _loggerFactory));
        }
    }
}