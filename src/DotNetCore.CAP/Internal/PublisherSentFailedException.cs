using System;

namespace DotNetCore.CAP.Internal
{
    public class PublisherSentFailedException : Exception
    {
        public PublisherSentFailedException(string message, Exception ex) : base(message, ex)
        {

        }
    }
}
