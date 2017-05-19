using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cap.Consistency.Routing;

namespace Cap.Consistency.Builder
{
    public class ConsistencyMiddleware
    {
        private readonly ITopicRoute _router;

        public ConsistencyMiddleware(ITopicRoute router) {
            _router = router;
        }

        public async Task Invoke() {
            var context = new TopicRouteContext();
            context.Routes.Add(_router);

            await _router.RouteAsync(context);
        }
    }
}
