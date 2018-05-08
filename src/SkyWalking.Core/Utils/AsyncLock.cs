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

namespace SkyWalking.Utils
{
    internal class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Releaser _releaser;
        private readonly Task<Releaser> _releaserTask;

        public AsyncLock()
        {
            _releaser = new Releaser(this);
            _releaserTask = Task.FromResult(_releaser);
        }

        public Task<Releaser> LockAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var wait = _semaphore.WaitAsync(cancellationToken);

            return wait.IsCompleted
                ? _releaserTask
                : wait.ContinueWith(
                    (_, state) => ((AsyncLock) state)._releaser,
                    this, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public Releaser Lock()
        {
            _semaphore.Wait();

            return _releaser;
        }

        public struct Releaser : IDisposable
        {
            private readonly AsyncLock _toRelease;

            internal Releaser(AsyncLock toRelease)
            {
                _toRelease = toRelease;
            }

            public void Dispose()
                => _toRelease._semaphore.Release();
        }
    }
}