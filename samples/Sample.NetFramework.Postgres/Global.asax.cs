using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace Sample.NetFramework.Postgres
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_End(object sender, EventArgs e)
        {
            // clean up CAP bootstrapper (created in Startup.StartCAP method)
            if (Application["CAPBootstrapper"] != null)
            {
                Application.Lock();
                var bootstrapper = Application["CAPBootstrapper"] as DotNetCore.CAP.Internal.Bootstrapper;

                if (bootstrapper != null)
                {
                    Task.Run(async () => { await bootstrapper.StopAsync(CancellationToken.None); }).Wait();
                    bootstrapper.Dispose();
                }

                Application["CAPBootstrapper"] = null;
                Application.UnLock();
            }
        }
    }
}
