using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sample.Dashboard.Auth
{
    public static class MyDashboardAuthenticationSchemeDefaults
    {
        public const string Scheme = "MyDashboardAuthenticationScheme";
    }
    
    public class MyDashboardAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {

    }

    public class MyDashboardAuthenticationHandler : AuthenticationHandler<MyDashboardAuthenticationSchemeOptions>
    {
        public MyDashboardAuthenticationHandler(IOptionsMonitor<MyDashboardAuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
            options.CurrentValue.ForwardChallenge = "";
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var testAuthHeaderPresent = Request.Headers["X-Base-Token"].Contains("xxx");

            var authResult = testAuthHeaderPresent ? CreateAuthenticatonTicket() : AuthenticateResult.NoResult();

            return Task.FromResult(authResult);
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = MyDashboardAuthenticationSchemeDefaults.Scheme;
            return base.HandleChallengeAsync(properties);
        }

        private AuthenticateResult CreateAuthenticatonTicket()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "My Dashboard user") };
            var identity = new ClaimsIdentity(claims, MyDashboardAuthenticationSchemeDefaults.Scheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, MyDashboardAuthenticationSchemeDefaults.Scheme);

            return AuthenticateResult.Success(ticket);
        }
    }
}
