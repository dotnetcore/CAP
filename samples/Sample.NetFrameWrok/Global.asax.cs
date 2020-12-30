using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.Mvc;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Sample.NetFramewrok.Services;
using Sample.NetFramewrok.Services.Impl;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Sample.NetFramewrok
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ISubscriberService, SubscriberService>();

            services.AddCap(x =>
            {
                x.UseSqlServer(
                    "Data Source=47.96.79.245;Initial Catalog=YY_TEST1;User ID=sa;Password=DB1qaz@WSX;MultipleActiveResultSets=True");
                x.UseRabbitMQ("127.0.0.1");
            });
            var sp = services.BuildServiceProvider();

            sp.GetService<IBootstrapper>()?.BootstrapAsync(default);

            var builder = new ContainerBuilder();
            builder.Populate(services);

            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            // MVC - OPTIONAL: Register model binders that require DI.
            builder.RegisterModelBinders(typeof(MvcApplication).Assembly);
            builder.RegisterModelBinderProvider();

            // MVC - OPTIONAL: Register web abstractions like HttpContextBase.
            builder.RegisterModule<AutofacWebTypesModule>();

            // MVC - OPTIONAL: Enable property injection in view pages.
            builder.RegisterSource(new ViewRegistrationSource());

            // MVC - OPTIONAL: Enable property injection into action filters.
            builder.RegisterFilterProvider();


            // MVC - Set the dependency resolver to be Autofac.
            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}