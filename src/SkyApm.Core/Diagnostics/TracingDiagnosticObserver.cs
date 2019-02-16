/*
 * Licensed to the SkyAPM under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The SkyAPM licenses this file to You under the Apache License, Version 2.0
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
using SkyApm.Logging;

namespace SkyApm.Diagnostics
{
    internal class TracingDiagnosticObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly TracingDiagnosticMethodCollection _methodCollection;
        private readonly ILogger _logger;

        public TracingDiagnosticObserver(ITracingDiagnosticProcessor tracingDiagnosticProcessor,
            ILoggerFactory loggerFactory)
        {
            _methodCollection = new TracingDiagnosticMethodCollection(tracingDiagnosticProcessor);
            _logger = loggerFactory.CreateLogger(typeof(TracingDiagnosticObserver));
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            foreach (var method in _methodCollection)
            {
                try
                {
                    method.Invoke(value.Key, value.Value);
                }
                catch (Exception exception)
                {
                    _logger.Error("Invoke diagnostic method exception.", exception);
                }
            }
        }
    }
}