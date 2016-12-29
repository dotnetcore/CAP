namespace Cap.Consistency.EventBus
{
    public interface IEventBusFactory
    {
        IEventBus CreateEventBus<TEventBus>() where TEventBus : IEventBus;

        IEventBus CreateEventBus<TEventBus>(long maxPendingEventNumber) where TEventBus : IEventBus;
    }
}