using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cap.Consistency.Infrastructure
{
    public static class WaitHandleEx
    {
        public static readonly AutoResetEvent PulseEvent = new AutoResetEvent(true);

        public static Task WaitAnyAsync(WaitHandle handle1, WaitHandle handle2, TimeSpan timeout) {
            var t1 = handle1.WaitOneAsync(timeout);
            var t2 = handle2.WaitOneAsync(timeout);
            return Task.WhenAny(t1, t2);
        }

        public static async Task<bool> WaitOneAsync(this WaitHandle handle, TimeSpan timeout) {
            RegisteredWaitHandle registeredHandle = null;
            try {
                var tcs = new TaskCompletionSource<bool>();
                registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                    handle,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut),
                    tcs,
                    timeout,
                    true);
                return await tcs.Task;
            }
            finally {
                if (registeredHandle != null) {
                    registeredHandle.Unregister(null);
                }
            }
        }
    }
}
