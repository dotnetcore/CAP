using System;

namespace DotNetCore.CAP.Internal
{
    internal class SubscriberExecutionFailedException : Exception
    {
        public SubscriberExecutionFailedException(string message, Exception ex) : base(message, ex)
        {

        }
    }
}
