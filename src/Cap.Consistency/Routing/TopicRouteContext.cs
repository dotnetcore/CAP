using System;
using System.Collections.Generic; 

namespace Cap.Consistency.Routing
{
    public class TopicRouteContext
    {
        public IServiceProvider ServiceProvider { get; set; }

        public IList<ITopicRoute> Routes { get; set; }

    }
}
