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
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SkyWalking.Context
{
    public static class ExceptionExtensions
    {
        public static string ConvertToString(Exception exception, int maxLength)
        {
            var message = new StringBuilder();

            while (exception != null)
            {
                message.Append(exception.Message);

                PrintStackFrame(message, exception.StackTrace, maxLength, out var overMaxLength);

                if (overMaxLength)
                {
                    break;
                }

                exception = exception.InnerException;
            }

            return message.ToString();
        }

        private static void PrintStackFrame(StringBuilder message, string stackTrace,
            int maxLength, out bool overMaxLength)
        {
            message.AppendLine(stackTrace);
            overMaxLength = message.Length > maxLength;
        }
    }
}
