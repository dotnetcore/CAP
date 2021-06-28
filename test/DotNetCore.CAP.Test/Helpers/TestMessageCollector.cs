using System.Collections.Generic;

namespace DotNetCore.CAP.Test.Helpers
{
    public class TestMessageCollector
    {
        private readonly ICollection<object> _handledMessages;

        public TestMessageCollector(ICollection<object> handledMessages)
        {
            _handledMessages = handledMessages;
        }

        public void Add(object data)
        {
            _handledMessages.Add(data);
        }
    }
}