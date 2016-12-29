using System;

namespace Cap.Consistency.EventBus
{
    /// <summary>
    /// The event bus interface.
    /// </summary>
    public interface IEventBus : IBackgroundWorker
    {
        /// <summary>
        /// Post an event to the event bus, dispatched after the specific time.
        /// </summary>
        /// <param name="eventObject">The event object</param>
        /// <param name="dispatchDelay">The delay time before dispatch this event</param>
        void Post(object eventObject, TimeSpan dispatchDelay);

        /// <summary>
        /// Register event handlers in the handler instance.
        ///
        /// One handler instance may have many event handler methods.
        /// These methods have EventSubscriberAttribute contract and exactly one parameter.
        /// </summary>
        /// <param name="handler">The instance of event handler class</param>
        void Register(object handler);

        /// <summary>
        /// Unregister event handlers belong to the handler instance.
        ///
        /// One handler instance may have many event handler methods.
        /// These methods have EventSubscriberAttribute contract and exactly one parameter.
        /// </summary>
        /// <param name="handler">The instance of event handler class</param>
        void Unregister(object handler);
    }
}