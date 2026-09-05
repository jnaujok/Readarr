using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NzbDrone.Core.Configuration;

namespace Readarr.Http.Authentication
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "API Key";

        public string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;

        public string HeaderName { get; set; }
        public string QueryName { get; set; }
        public bool AllowQueryString { get; set; }
    }

    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private readonly string _apiKey;

        public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfigFileProvider config)
            : base(options, logger, encoder)
        {
            _apiKey = config.ApiKey;
        }

        private string ParseApiKey()
        {
            if (!string.IsNullOrEmpty(Options.QueryName) &&
                Request.Query.TryGetValue(Options.QueryName, out var value) &&
                AllowQueryKey())
            {
                return value.FirstOrDefault();
            }

            if (Request.Headers.TryGetValue(Options.HeaderName, out var headerValue))
            {
                return headerValue.FirstOrDefault();
            }

            return Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        }

        private bool AllowQueryKey()
        {
            if (Options.AllowQueryString)
            {
                return true;
            }

            var path = Request.Path.Value ?? string.Empty;
            return path.StartsWith("/feed", StringComparison.OrdinalIgnoreCase);
        }

        private bool KeysMatch(string provided)
        {
            var providedBytes = Encoding.UTF8.GetBytes(provided);
            var expectedBytes = Encoding.UTF8.GetBytes(_apiKey ?? string.Empty);

            if (providedBytes.Length != expectedBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var providedApiKey = ParseApiKey();

            if (string.IsNullOrWhiteSpace(providedApiKey))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (KeysMatch(providedApiKey))
            {
                var claims = new List<Claim>
                {
                    new Claim("ApiKey", "true")
                };

                var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
                var identities = new List<ClaimsIdentity> { identity };
                var principal = new ClaimsPrincipal(identities);
                var ticket = new AuthenticationTicket(principal, Options.Scheme);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            return Task.CompletedTask;
        }
    }
}
