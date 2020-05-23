// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Internal
{
    public static class Helper
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);

        public static long ToTimestamp(DateTime value)
        {
            var elapsedTime = value - Epoch;
            return (long) elapsedTime.TotalSeconds;
        }

        public static bool IsController(TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass)
            {
                return false;
            }

            if (typeInfo.IsAbstract)
            {
                return false;
            }

            if (!typeInfo.IsPublic)
            {
                return false;
            }

            return !typeInfo.ContainsGenericParameters
                   && typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsComplexType(Type type)
        {
            return !CanConvertFromString(type);
        }

        public static bool IsInnerIP(string ipAddress)
        {
            bool isInnerIp;
            var ipNum = GetIpNum(ipAddress);

            //Private IP：
            //category A: 10.0.0.0-10.255.255.255
            //category B: 172.16.0.0-172.31.255.255
            //category C: 192.168.0.0-192.168.255.255  

            var aBegin = GetIpNum("10.0.0.0");
            var aEnd = GetIpNum("10.255.255.255");
            var bBegin = GetIpNum("172.16.0.0");
            var bEnd = GetIpNum("172.31.255.255");
            var cBegin = GetIpNum("192.168.0.0");
            var cEnd = GetIpNum("192.168.255.255");
            isInnerIp = IsInner(ipNum, aBegin, aEnd) || IsInner(ipNum, bBegin, bEnd) || IsInner(ipNum, cBegin, cEnd);
            return isInnerIp;
        }

        private static long GetIpNum(string ipAddress)
        {
            var ip = ipAddress.Split('.');
            long a = int.Parse(ip[0]);
            long b = int.Parse(ip[1]);
            long c = int.Parse(ip[2]);
            long d = int.Parse(ip[3]);

            var ipNum = a * 256 * 256 * 256 + b * 256 * 256 + c * 256 + d;
            return ipNum;
        }

        private static bool IsInner(long userIp, long begin, long end)
        {
            return userIp >= begin && userIp <= end;
        }

        private static bool CanConvertFromString(Type destinationType)
        {
            destinationType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
            return IsSimpleType(destinationType) ||
                   TypeDescriptor.GetConverter(destinationType).CanConvertFrom(typeof(string));
        }

        private static bool IsSimpleType(Type type)
        {
            return type.GetTypeInfo().IsPrimitive ||
                   type == typeof(decimal) ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(Guid) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Uri);
        }

        public static async Task<TResult> TimeOutExecuteAsync<TResult>(
            int seconds,
            Func<CancellationToken, Task<TResult>> action)
        {
            if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            Func<TimeSpan, Task, Exception, Task> doNothingAsync = (_, __, ___) => Task.FromResult(true);
            return await TimeOutExecuteAsync(TimeSpan.FromSeconds(seconds), action, CancellationToken.None,
                doNothingAsync, false);
        }

        public static TResult TimeOutExecute<TResult>(
            int seconds,
            Func<CancellationToken, TResult> action)
        {
            if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            Action<TimeSpan, Task, Exception> doNothingAsync = (_, __, ___) => { };
            return TimeOutExecute(TimeSpan.FromSeconds(seconds), action, CancellationToken.None,
                doNothingAsync);
        }

        public static async Task<TResult> TimeOutExecuteAsync<TResult>(
            TimeSpan timeout,
            Func<CancellationToken, Task<TResult>> action,
            CancellationToken cancellationToken,
            Func<TimeSpan, Task, Exception, Task> onTimeoutAsync,
            bool continueOnCapturedContext)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                using (CancellationTokenSource combinedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                        timeoutCancellationTokenSource.Token))
                {
                    Task<TResult> actionTask = null;
                    CancellationToken combinedToken = combinedTokenSource.Token;
                    try
                    {
                        Task<TResult> timeoutTask = AsTask<TResult>(timeoutCancellationTokenSource.Token);
                        timeoutCancellationTokenSource.CancelAfter(timeout);
                        actionTask = action(combinedToken);
                        return await (await Task.WhenAny(actionTask, timeoutTask)
                            .ConfigureAwait(continueOnCapturedContext)).ConfigureAwait(continueOnCapturedContext);
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException && timeoutCancellationTokenSource.IsCancellationRequested)
                        {
                            await onTimeoutAsync(timeout, actionTask, ex)
                                .ConfigureAwait(continueOnCapturedContext);
                            throw new TimeoutException(
                                "The executed delegate did not complete in the timeout. Check whether there is a dead loop or a time-consuming operation in the delegate",
                                ex);
                        }

                        throw;
                    }
                }
            }
        }

        public static TResult TimeOutExecute<TResult>(
            TimeSpan timeout,
            Func<CancellationToken, TResult> action,
            CancellationToken cancellationToken,
            Action<TimeSpan, Task, Exception> onTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                using (CancellationTokenSource combinedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                        timeoutCancellationTokenSource.Token))
                {
                    CancellationToken combinedToken = combinedTokenSource.Token;

                    Task<TResult> actionTask = null;
                    try
                    {
                        timeoutCancellationTokenSource.CancelAfter(timeout);
                        actionTask = Task.Run(() =>
                                action(
                                    combinedToken) // cancellation token here allows the user delegate to react to cancellation: possibly clear up; then throw an OperationCanceledException.
                            , combinedToken); // cancellation token here only allows Task.Run() to not begin the passed delegate at all, if cancellation occurs prior to invoking the delegate.
                        try
                        {
                            actionTask.Wait(timeoutCancellationTokenSource
                                .Token); // cancellation token here cancels the Wait() and causes it to throw, but does not cancel actionTask.  We use only timeoutCancellationTokenSource.Token here, not combinedToken.  If we allowed the user's cancellation token to cancel the Wait(), in this pessimistic scenario where the user delegate may not observe that cancellation, that would create a no-longer-observed task.  That task could in turn later fault before completing, risking an UnobservedTaskException.
                        }
                        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1
                        ) // Issue #270.  Unwrap extra AggregateException caused by the way pessimistic timeout policy for synchronous executions is necessarily constructed.
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }

                        return actionTask.Result;
                    }
                    catch (Exception ex)
                    {
                        // Note that we cannot rely on testing (operationCanceledException.CancellationToken == combinedToken || operationCanceledException.CancellationToken == timeoutCancellationTokenSource.Token)
                        // as either of those tokens could have been onward combined with another token by executed code, and so may not be the token expressed on operationCanceledException.CancellationToken.
                        if (ex is OperationCanceledException && timeoutCancellationTokenSource.IsCancellationRequested)
                        {
                            onTimeout(timeout, actionTask, ex);
                            throw new TimeoutException(
                                "The executed delegate did not complete in the timeout. Check whether there is a dead loop or a time-consuming operation in the delegate", ex);
                        }

                        throw;
                    }
                }
            }
        }

        public static Task<TResult> AsTask<TResult>(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<TResult>();
            cancellationToken.Register(() => { tcs.TrySetCanceled(); },
                useSynchronizationContext: false);
            return tcs.Task;
        }
    }
}