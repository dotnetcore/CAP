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
using SkyWalking.Logging;

namespace SkyWalking.AspNet.Logging
{
    internal class DebugLoggerAdapter : ILogger
    {
        private readonly Type type;

        public DebugLoggerAdapter(Type type)
        {
            this.type = type;
        }

        public void Debug(string message)
        {
            WriteLine("debug", message);
        }

        public void Info(string message)
        {
            WriteLine("info", message);
        }

        public void Warning(string message)
        {
            WriteLine("warn", message);
        }

        public void Error(string message, Exception exception)
        {
            WriteLine("error", message + Environment.NewLine + exception);
        }

        public void Trace(string message)
        {
            WriteLine("trace", message);
        }

        private void WriteLine(string level, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now} : [{level}] [{type.Name}] {message}");
        }
    }
}