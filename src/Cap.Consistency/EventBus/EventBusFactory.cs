using System;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency.EventBus
{
    public class EventBusFactory
        : IEventBusFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public EventBusFactory(ILoggerFactory loggerFactory) {
            this._loggerFactory = loggerFactory;
        }

        public IEventBus CreateEventBus<TEventBus>() where TEventBus : IEventBus {
            return this.CreateEventBus<TEventBus>(-1);
        }

        public IEventBus CreateEventBus<TEventBus>(long maxPendingEventNumber) where TEventBus : IEventBus {
            return Activator.CreateInstance(typeof(TEventBus), new object[] { this._loggerFactory, maxPendingEventNumber }) as IEventBus;
        }
    }
}