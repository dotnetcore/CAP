using System;

namespace DotNetCore.CAP.Internal
{
    public class SubscriberNotFoundException : Exception
    {
        public SubscriberNotFoundException()
        {
        }

        public SubscriberNotFoundException(string message) : base(message)
        {
        }

        public SubscriberNotFoundException(string message, Exception inner) :
            base(message, inner)
        {
        }
    }
}