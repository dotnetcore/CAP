using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Abstractions
{
 
    public class ConsumerContext
    {
        public ConsumerContext(ConsumerExecutorDescriptor descriptor) {
            ConsumerDescriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        }

        public ConsumerExecutorDescriptor ConsumerDescriptor { get; set; }


     
    }
}
