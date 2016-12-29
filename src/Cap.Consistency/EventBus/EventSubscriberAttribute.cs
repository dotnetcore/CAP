using System;

namespace Cap.Consistency.EventBus
{
    /// <summary>
    /// The attribute class of the event handler.
    /// If a method have this attribue contract and exactly one parameter, then it's event handler.
    /// </summary>
    public class EventSubscriberAttribute
        : Attribute
    {
    }
}