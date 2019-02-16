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
using Microsoft.Extensions.Logging;
using ILogger = SkyApm.Logging.ILogger;
using MSLogger = Microsoft.Extensions.Logging.ILogger;

namespace SkyApm.Utilities.Logging
{
    internal class DefaultLogger : SkyApm.Logging.ILogger
    {
        private readonly MSLogger _readLogger;

        public DefaultLogger(MSLogger readLogger)
        {
            _readLogger = readLogger;
        }

        public void Debug(string message)
        {
            _readLogger.LogDebug(message);
        }

        public void Information(string message)
        {
            _readLogger.LogInformation(message);
        }

        public void Warning(string message)
        {
            _readLogger.LogWarning(message);
        }

        public void Error(string message, Exception exception)
        {
            _readLogger.LogError(message + Environment.NewLine + exception);
        }

        public void Trace(string message)
        {
            _readLogger.LogTrace(message);
        }
    }
}