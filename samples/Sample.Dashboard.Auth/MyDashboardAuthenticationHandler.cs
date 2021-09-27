using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sample.Dashboard.Auth
{
    public class MyDashboardAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {

    }

    public class MyDashboardAuthenticationHandler : AuthenticationHandler<MyDashboardAuthenticationSchemeOptions>
    {
        public MyDashboardAuthenticationHandler(IOptionsMonitor<MyDashboardAuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            options.CurrentValue.ForwardChallenge = "";
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var testAuthHeaderPresent = Request.Headers["X-Base-Token"].Contains("xxx");

            var authResult = testAuthHeaderPresent ? AuthenticatedTestUser() : AuthenticateResult.NoResult();

            return Task.FromResult(authResult);
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = "MyDashboardScheme";

            return base.HandleChallengeAsync(properties);
        }

        private AuthenticateResult AuthenticatedTestUser()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "My Dashboard user") };
            var identity = new ClaimsIdentity(claims, "MyDashboardScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "MyDashboardScheme");

            return AuthenticateResult.Success(ticket);
        }
    }
}
