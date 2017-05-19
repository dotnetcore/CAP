using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Routing
{
    public interface ITopicRoute
    {
        Task RouteAsync(TopicRouteContext context);
    }
}
