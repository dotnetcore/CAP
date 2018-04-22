/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
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
using SkyWalking.Utils;

namespace SkyWalking.Diagnostics
{
    public abstract class TracingDiagnosticObserver : IObserver<DiagnosticListener>
    {
        private readonly IEnumerable<ITracingDiagnosticProcessor> _tracingDiagnosticProcessors;

        public TracingDiagnosticObserver(IEnumerable<ITracingDiagnosticProcessor> tracingDiagnosticProcessors)
        {
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
                    OnNext(listener, diagnosticProcessor);
                }
            }
        }

        protected abstract void OnNext(DiagnosticListener listener,
            ITracingDiagnosticProcessor diagnosticProcessor);
    }
}