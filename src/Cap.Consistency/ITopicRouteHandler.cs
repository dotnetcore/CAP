using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency
{
    public interface ITopicRouteHandler
    {
        Task RouteAsync(TopicRouteContext context);
    }
}
