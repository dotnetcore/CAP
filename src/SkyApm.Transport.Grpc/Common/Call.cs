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
using System.Threading.Tasks;
using SkyApm.Logging;

namespace SkyApm.Transport.Grpc.Common
{
    internal class Call
    {
        private readonly ILogger _logger;
        private readonly ConnectionManager _connectionManager;

        public Call(ILogger logger, ConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public async Task Execute(Func<Task> task, Func<string> errMessage)
        {
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                _logger.Error(errMessage(), ex);
                _connectionManager.Failure(ex);
            }
        }

        public async Task<T> Execute<T>(Func<Task<T>> task, Func<T> errCallback, Func<string> errMessage)
        {
            try
            {
                return await task();
            }
            catch (Exception ex)
            {
                _logger.Error(errMessage(), ex);
                _connectionManager.Failure(ex);
                return errCallback();
            }
        }
    }
}