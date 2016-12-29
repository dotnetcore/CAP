using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cap.Consistency.EventBus
{
    /// <summary>
    /// The OrderedEventBus class is a simple and fast IEventBus implementation which processes event in the delivery order.
    /// </summary>
    /// <remarks>If you do not need the event processed in the delivery order, use SimpleEventBus instead.</remarks>
    public class OrderedEventBus
        : EventBusBase, IEventBus
    {
        private readonly BlockingCollection<object> _eventQueue;

        /// <summary>
        /// The pending event number which does not yet dispatched.
        /// </summary>
        public override long PendingEventNumber {
            get {
                return Math.Max(_eventQueue.Count, 0);
            }
        }

        public override bool IsDispatcherEnabled {
            get {
                return true;
            }
        }

        public OrderedEventBus(ILoggerFactory loggerFactory, IOptions<EventBusOptions> options)
            : this(loggerFactory, options?.Value.MaxPendingEventNumber32 ?? 0) {
        }

        /// <summary>
        /// The constructor of OrderedEventBus.
        /// </summary>
        /// <param name="maxPendingEventNumber">The maximum pending event number which does not yet dispatched</param>
        public OrderedEventBus(ILoggerFactory loggerFactory, int maxPendingEventNumber, bool shouldStart = true)
            : base(loggerFactory) {
            this._eventQueue = new BlockingCollection<object>(
                maxPendingEventNumber > 0
                ? maxPendingEventNumber
                : DefaultMaxPendingEventNumber);

            if (shouldStart) {
                this.Start();
            }
        }

        /// <summary>
        /// Post an event to the event bus, dispatched after the specific time.
        /// </summary>
        /// <remarks>If you do not need the event processed in the delivery order, use SimpleEventBus instead.</remarks>
        /// <param name="eventObject">The event object</param>
        /// <param name="dispatchDelay">The delay time before dispatch this event</param>
        public override void Post(object eventObject, TimeSpan dispatchDelay) {
            int dispatchDelayMs = (int)dispatchDelay.TotalMilliseconds;

            if (dispatchDelayMs >= 1) {
                Task.Delay(dispatchDelayMs).ContinueWith(task => _eventQueue.Add(eventObject));
            }
            else {
                _eventQueue.Add(eventObject);
            }
        }

        protected override async Task<bool> Process() {
            object eventObject = null;
            try {
                eventObject = _eventQueue.Take();
                InvokeEventHandler(eventObject);
            }
            catch (Exception de) {
                if (de is ObjectDisposedException) {
                    return await Task.FromResult(true);
                }
                this._logger.LogError("Dispatch event ({0}) failed: {1}{2}{3}", eventObject, de.Message, Environment.NewLine, de.StackTrace);
            }
            return await Task.FromResult(true);
        }

        protected void InvokeEventHandler(object eventObject, Action<bool, Exception, object, Type> resultCallback = null) {
            List<Task> taskList = null;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < _eventHandlerList.Count; i++) {
                // ReSharper disable once InconsistentlySynchronizedField
                EventHandlerHolder record = _eventHandlerList[i];
                if (eventObject == null || record.ParameterType.IsInstanceOfType(eventObject)) {
                    Task task = Task.Run(() => {
                        this.OnMessageReceieved(record);
                        var isVoid = record.MethodInfo.ReturnType == typeof(void);
                        try {
                            var result = record.MethodInfo.Invoke(record.Handler, new[] { eventObject });
                            resultCallback?.Invoke(isVoid, null, result, record.MethodInfo.ReturnType);
                        }
                        catch (Exception ex) {
                            this._logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                            resultCallback?.Invoke(isVoid, ex, null, record.MethodInfo.ReturnType);
                        }
                    });
                    if (taskList == null) taskList = new List<Task>();
                    taskList.Add(task);
                    //record.MethodInfo.Invoke(record.Handler, new[] { eventObject });
                }
            }
            if (taskList != null) {
                Task.WaitAll(taskList.ToArray());
            }
            else {
                resultCallback?.Invoke(false, null, null, null);
            }
        }
    }
}