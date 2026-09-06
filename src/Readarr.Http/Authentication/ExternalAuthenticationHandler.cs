using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Authentication;

namespace Readarr.Http.Authentication
{
    public class ExternalAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public ExternalAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var username = Request.Headers["Remote-User"].FirstOrDefault()
                           ?? Request.Headers["X-Remote-User"].FirstOrDefault()
                           ?? Request.Headers["X-Forwarded-User"].FirstOrDefault();

            if (username.IsNullOrWhiteSpace())
            {
                return Task.FromResult(AuthenticateResult.Fail("External authentication requires a Remote-User header from a trusted reverse proxy."));
            }

            var claims = new List<Claim>
            {
                new Claim("user", username),
                new Claim("AuthType", AuthenticationType.External.ToString())
            };

            var identity = new ClaimsIdentity(claims, "External", "user", "identifier");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
