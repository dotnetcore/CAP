using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency.EventBus
{
    /// <summary>
    /// The EventBusBase class is the base class for all the IEventBus implementations.
    /// </summary>
    public abstract class EventBusBase
        : BackgroundWorker, IEventBus, IDisposable
    {
        public const int DefaultMaxPendingEventNumber = 1024 * 1024;

        public event EventHandler<EventHandlerHolder> MessageReceieved;

        protected readonly object _eventHandlerLock = new object();

        protected List<EventHandlerHolder> _eventHandlerList = new List<EventHandlerHolder>();

        /// <summary>
        /// The pending event number which does not yet dispatched.
        /// </summary>
        public abstract long PendingEventNumber { get; }

        public virtual bool IsDispatcherEnabled {
            get {
                return base.IsRunning;
            }
        }

        /// <summary>
        /// The constructor of EventBusBase.
        /// </summary>
        /// <param name="loggerFactory"></param>
        protected EventBusBase(ILoggerFactory loggerFactory)
            : base(loggerFactory) {
            this._eventHandlerList = new List<EventHandlerHolder>();
        }

        /// <summary>
        /// Post an event to the event bus, dispatched after the specific time.
        /// </summary>
        /// <remarks>If you do not need the event processed in the delivery order, use SimpleEventBus instead.</remarks>
        /// <param name="eventObject">The event object</param>
        /// <param name="dispatchDelay">The delay time before dispatch this event</param>
        public abstract void Post(object eventObject, TimeSpan dispatchDelay);

        /// <summary>
        /// Register event handlers in the handler instance.
        /// One handler instance may have many event handler methods.
        /// These methods have EventSubscriberAttribute contract and exactly one parameter.
        /// </summary>
        /// <remarks>If you do not need the event processed in the delivery order, use SimpleEventBus instead.</remarks>
        /// <param name="handler">The instance of event handler class</param>
        public void Register(object handler) {
            if (handler == null) {
                return;
            }

            var miList = handler.GetType().GetRuntimeMethods();
            lock (_eventHandlerLock) {
                // Don't allow register multiple times.
                if (_eventHandlerList.Any(record => record.Handler == handler)) {
                    return;
                }

                List<EventHandlerHolder> newList = null;
                foreach (var mi in miList) {
                    var attribute = mi.GetCustomAttribute<EventSubscriberAttribute>();
                    if (attribute != null) {
                        var piList = mi.GetParameters();
                        if (piList.Length == 1) {
                            // OK, we got valid handler, create newList as needed
                            if (newList == null) {
                                newList = new List<EventHandlerHolder>(_eventHandlerList);
                            }
                            newList.Add(this.CreateEventHandlerHolder(handler, mi, piList[0].ParameterType));
                        }
                    }
                }

                // OK, we have new handler registered
                if (newList != null) {
                    _eventHandlerList = newList;
                }
            }
        }

        /// <summary>
        /// Unregister event handlers belong to the handler instance.
        /// One handler instance may have many event handler methods.
        /// These methods have EventSubscriberAttribute contract and exactly one parameter.
        /// </summary>
        /// <param name="handler">The instance of event handler class</param>
        public void Unregister(object handler) {
            if (handler == null) {
                return;
            }
            lock (_eventHandlerLock) {
                bool needAction = _eventHandlerList.Any(record => record.Handler == handler);

                if (needAction) {
                    var newList = new List<EventHandlerHolder>();
                    foreach (var record in this._eventHandlerList) {
                        if (record.Handler != handler) {
                            newList.Add(record);
                        }
                        else {
                            record.Dispose();
                        }
                    }
                    _eventHandlerList = newList;
                }
            }
        }

        protected virtual EventHandlerHolder CreateEventHandlerHolder(object handler, MethodInfo methodInfo, Type parameterType) {
            return new EventHandlerHolder(handler, methodInfo, parameterType);
        }

        protected virtual void OnMessageReceieved(EventHandlerHolder handler) {
            this.MessageReceieved?.Invoke(this, handler);
        }

        #region IDisposable

        // Flag: Has Dispose already been called?
        private bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public new void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected new virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                // Free any other managed objects here.
                this.Stop();
            }

            // Free any unmanaged objects here.
            disposed = true;
        }

        #endregion IDisposable
    }
}