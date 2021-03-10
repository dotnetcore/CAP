using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.RabbitMQ.Postgres.DashboardAuth
{
    public class HttpContextDashboardFilter : IDashboardAuthorizationFilter
    {
        public Task<bool> AuthorizeAsync(DashboardContext context)
        {
            var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();

            if (httpContextAccessor is null)
                throw new ArgumentException("Configure IHttpContextAccessor as a service on Startup");

            return Task.FromResult(httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true);
        }
    }
}