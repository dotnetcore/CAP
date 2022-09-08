using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Sample.NetFramework.Postgres
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Publish", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "DefaultNoArgs",
                url: "",
                defaults: new { controller = "Home", action = "Publish" }
            );
        }
    }
}
