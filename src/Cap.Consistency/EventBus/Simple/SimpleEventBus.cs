//#define UseTotalEventNumber

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cap.Consistency.EventBus
{
    /// <summary>
    /// The SimpleEventBus class is a simple and fast IEventBus implementation.
    /// </summary>
    /// <remarks>
    /// <para>The event may be processed out of the delivery order under heavy load.</para>
    /// <para>If you need the event processed in the delivery order, use OrderedEventBus instead.</para>
    /// </remarks>
    public class SimpleEventBus
        : EventBusBase, IEventBus
    {
        private readonly long _maxPendingEventNumber;

        // Interlocked operation cause the performance drop at least 10% !.
        private long _pendingEventNumber;

        private bool _isDispatcherEnabled;

#if UseTotalEventNumber
        // This counter cause the performance drop at least 5% !
        private long _totalEventNumber;

        // The total event number which post to the event bus.
        // This counter cause the performance drop at least 5% !
        public long TotalEventNumber
        {
            get
            {
                return Interlocked.Read(ref _totalEventNumber);
            }
        }
#endif

        /// <summary>
        /// The pending event number which does not yet dispatched.
        /// </summary>
        public override long PendingEventNumber {
            get {
                return Math.Max(Interlocked.Read(ref _pendingEventNumber), 0);
            }
        }

        public override bool IsDispatcherEnabled {
            get {
                return this._isDispatcherEnabled;
            }
        }

        public SimpleEventBus(ILoggerFactory loggerFactory, IOptions<EventBusOptions> options)
            : this(loggerFactory, options?.Value.MaxPendingEventNumber ?? 0) {
        }

        /// <summary>
        /// The constructor of SimpleEventBus.
        /// </summary>
        /// <param name="maxPendingEventNumber">The maximum pending event number which does not yet dispatched</param>
        public SimpleEventBus(ILoggerFactory loggerFactory, long maxPendingEventNumber, bool shouldStart = true)
            : base(loggerFactory) {
            this._maxPendingEventNumber = maxPendingEventNumber > 0 ? maxPendingEventNumber : DefaultMaxPendingEventNumber;
            this._isDispatcherEnabled = false;

            if (shouldStart) {
                this.Start();
            }
        }

        public override void Start() {
            if (this.IsRunning) {
                return;
            }
            this._isDispatcherEnabled = true;
        }

        public override void Stop(int timeout = 2000) {
            this._isDispatcherEnabled = false;
        }

        /// <summary>
        /// Post an event to the event bus, dispatched after the specific time.
        /// </summary>
        /// <remarks>
        /// <para>The event may be processed out of the delivery order under heavy load.</para>
        /// <para>If you need the event processed in the delivery order, use OrderedEventBus instead.</para>
        /// </remarks>
        /// <param name="eventObject">The event object</param>
        /// <param name="dispatchDelay">The delay time before dispatch this event</param>
        public override void Post(object eventObject, TimeSpan dispatchDelay) {
            if (!this._isDispatcherEnabled) return;

            int dispatchDelayMs = (int)dispatchDelay.TotalMilliseconds;

            while (Interlocked.Read(ref _pendingEventNumber) >= _maxPendingEventNumber) {
                this._logger.LogWarning("Too many events in the EventBus, pendingEventNumber={0}, maxPendingEventNumber={1}{2}PendingEvent='{3}', dispatchDelay={4}ms",
                    PendingEventNumber, _maxPendingEventNumber, Environment.NewLine, eventObject, dispatchDelayMs);
                Task.Delay(16).Wait();
            }

            if (dispatchDelayMs >= 1) {
                Task.Delay(dispatchDelayMs).ContinueWith(task => {
                    DispatchMessage(eventObject);
                });
            }
            else {
                Task.Run(() => DispatchMessage(eventObject));
            }

            Interlocked.Increment(ref _pendingEventNumber);
            // Interlocked.Increment(ref _totalEventNumber);
        }

        protected override Task ThreadWorker(object userObject) {
            throw new NotSupportedException();
        }

        protected override Task<bool> Process() {
            throw new NotSupportedException();
        }

        protected void DispatchMessage(object eventObject) {
            try {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (int i = 0; i < _eventHandlerList.Count; i++) {
                    // ReSharper disable once InconsistentlySynchronizedField
                    EventHandlerHolder record = _eventHandlerList[i];
                    if (eventObject == null || record.ParameterType.IsInstanceOfType(eventObject)) {
                        Task.Run(() => {
                            try {
                                this.OnMessageReceieved(record);
                                record.MethodInfo.Invoke(record.Handler, new[] { eventObject });
                            }
                            catch (Exception ie) {
                                this._logger.LogWarning("Event handler (class '{0}@{1}', method '{2}') failed: {3}{4}{5}{4}eventObject: {6}",
                                    record.Handler.GetType(), record.Handler.GetHashCode(), record.MethodInfo,
                                    ie.Message, Environment.NewLine, ie.StackTrace, eventObject);
                            }
                        });
                    }
                }
            }
            catch (Exception de) {
                this._logger.LogError("Dispatch event ({0}) failed: {1}{2}{3}",
                    eventObject, de.Message, Environment.NewLine, de.StackTrace);
            }
            finally {
                Interlocked.Decrement(ref _pendingEventNumber);
            }
        }
    }
}