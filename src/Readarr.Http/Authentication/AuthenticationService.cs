using Microsoft.AspNetCore.Http;
using NLog;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using Readarr.Http.Extensions;

namespace Readarr.Http.Authentication
{
    public interface IAuthenticationService
    {
        void LogUnauthorized(HttpRequest context);
        User Login(HttpRequest request, string username, string password);
        void Logout(HttpContext context);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private static readonly Logger _authLogger = LogManager.GetLogger("Auth");
        private readonly IUserService _userService;
        private readonly LoginRateLimiter _loginRateLimiter;

        private static AuthenticationType AUTH_METHOD;

        public AuthenticationService(IConfigFileProvider configFileProvider, IUserService userService, LoginRateLimiter loginRateLimiter)
        {
            _userService = userService;
            _loginRateLimiter = loginRateLimiter;
            AUTH_METHOD = configFileProvider.AuthenticationMethod;
        }

        public User Login(HttpRequest request, string username, string password)
        {
            if (AUTH_METHOD == AuthenticationType.None)
            {
                return null;
            }

            var ip = request.GetRemoteIP();

            if (_loginRateLimiter.IsBlocked(ip))
            {
                _authLogger.Warn("Auth-Throttled ip {0} username '{1}'", ip, username);
                return null;
            }

            var user = _userService.FindUser(username, password);

            if (user != null)
            {
                _loginRateLimiter.RecordSuccess(ip);
                LogSuccess(request, username);

                return user;
            }

            _loginRateLimiter.RecordFailure(ip);
            LogFailure(request, username);

            return null;
        }

        public void Logout(HttpContext context)
        {
            if (AUTH_METHOD == AuthenticationType.None)
            {
                return;
            }

            if (context.User != null)
            {
                LogLogout(context.Request, context.User.Identity.Name);
            }
        }

        public void LogUnauthorized(HttpRequest context)
        {
            _authLogger.Info("Auth-Unauthorized ip {0} url '{1}'", context.GetRemoteIP(), context.Path);
        }

        private void LogInvalidated(HttpRequest context)
        {
            _authLogger.Info("Auth-Invalidated ip {0}", context.GetRemoteIP());
        }

        private void LogFailure(HttpRequest context, string username)
        {
            _authLogger.Warn("Auth-Failure ip {0} username '{1}'", context.GetRemoteIP(), username);
        }

        private void LogSuccess(HttpRequest context, string username)
        {
            _authLogger.Info("Auth-Success ip {0} username '{1}'", context.GetRemoteIP(), username);
        }

        private void LogLogout(HttpRequest context, string username)
        {
            _authLogger.Info("Auth-Logout ip {0} username '{1}'", context.GetRemoteIP(), username);
        }
    }
}
