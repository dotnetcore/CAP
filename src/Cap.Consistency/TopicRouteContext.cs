using System;
using System.Collections.Generic; 

namespace Cap.Consistency
{
    public class TopicRouteContext
    {
        public IServiceProvider ServiceProvider { get; set; }

        public IList<ITopicRouteHandler> Routes { get; set; }
    }
}
