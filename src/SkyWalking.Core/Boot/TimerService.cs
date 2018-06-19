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
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Logging;
using SkyWalking.Utils;

namespace SkyWalking.Boot
{
    public abstract class TimerService : IBootService
    {
        private static readonly ILogger _logger = LogManager.GetLogger<TimerService>();
        protected abstract TimeSpan Interval { get; }
        private Task _task;

        public virtual void Dispose()
        {
        }

        public virtual int Order { get; } = 2;

        public async Task Initialize(CancellationToken token)
        {
            await Initializing(token);
            _task = Task.Factory.StartNew(async () =>
                {
                    await Starting(token);
                    while (true)
                    {
                        try
                        {
                            await Execute(token);
                        }
                        catch (Exception e)
                        {
                            _logger.Error($"{GetType().Name} execute fail.", e);
                        }
                        await Task.Delay(Interval, token);
                    }  
                },
                token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        protected virtual Task Initializing(CancellationToken token)
        {
            return TaskUtils.CompletedTask;
        }

        protected virtual Task Starting(CancellationToken token)
        {
            return TaskUtils.CompletedTask;
        }
        
        protected abstract Task Execute(CancellationToken token);
    }
}