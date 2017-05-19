using System;
using System.Collections.Generic;
using System.Text;

namespace Cap.Consistency.Consumer
{
    public interface IConsumerHandler
    {
        void Start(IEnumerable<IConsumerService> consumers);

        void Stop();
    }

     
}
