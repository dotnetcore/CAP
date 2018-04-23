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

namespace SkyWalking.Logging
{
    public static class LogManager
    {
        private static readonly ILoggerFactory defaultLoggerFactory = new NullLoggerFactory();
        private static ILoggerFactory _loggerFactory;

        public static ILogger GetLogger(Type type)
        {
            var loggerFactory = _loggerFactory ?? defaultLoggerFactory;
            return loggerFactory.CreateLogger(type);
        }

        public static ILogger GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        public static void SetLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
    }
}