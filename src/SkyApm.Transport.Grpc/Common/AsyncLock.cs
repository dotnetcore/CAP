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
using System.Threading;
using System.Threading.Tasks;

namespace SkyApm.Transport.Grpc.Common
{
    internal class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Release _release;
        private readonly Task<Release> _releaseTask;

        public AsyncLock()
        {
            _release = new Release(this);
            _releaseTask = Task.FromResult(_release);
        }

        public Task<Release> LockAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var wait = _semaphore.WaitAsync(cancellationToken);

            return wait.IsCompleted
                ? _releaseTask
                : wait.ContinueWith(
                    (_, state) => ((AsyncLock) state)._release,
                    this, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public Release Lock()
        {
            _semaphore.Wait();

            return _release;
        }

        public struct Release : IDisposable
        {
            private readonly AsyncLock _toRelease;

            internal Release(AsyncLock toRelease)
            {
                _toRelease = toRelease;
            }

            public void Dispose()
                => _toRelease._semaphore.Release();
        }
    }
}