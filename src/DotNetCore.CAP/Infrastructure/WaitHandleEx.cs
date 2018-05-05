// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Infrastructure
{
    public static class WaitHandleEx
    {
        public static Task WaitAnyAsync(WaitHandle handle1, WaitHandle handle2, TimeSpan timeout)
        {
            var t1 = handle1.WaitOneAsync(timeout);
            var t2 = handle2.WaitOneAsync(timeout);
            return Task.WhenAny(t1, t2);
        }

        public static async Task<bool> WaitOneAsync(this WaitHandle handle, TimeSpan timeout)
        {
            RegisteredWaitHandle registeredHandle = null;
            try
            {
                var tcs = new TaskCompletionSource<bool>();
                registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                    handle,
                    (state, timedOut) => ((TaskCompletionSource<bool>) state).TrySetResult(!timedOut),
                    tcs,
                    timeout,
                    true);
                return await tcs.Task;
            }
            finally
            {
                registeredHandle?.Unregister(null);
            }
        }
    }
}