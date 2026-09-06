using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Configuration.Events;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ApplicationStartedEvent))]
    [CheckOn(typeof(ConfigSavedEvent))]
    public class AuthenticationDisabledCheck : HealthCheckBase
    {
        private readonly IConfigFileProvider _configFileProvider;

        public AuthenticationDisabledCheck(IConfigFileProvider configFileProvider, ILocalizationService localizationService)
            : base(localizationService)
        {
            _configFileProvider = configFileProvider;
        }

        public override HealthCheck Check()
        {
            if (_configFileProvider.AuthenticationMethod != AuthenticationType.None)
            {
                return new HealthCheck(GetType());
            }

            return new HealthCheck(
                GetType(),
                HealthCheckResult.Error,
                _localizationService.GetLocalizedString("AuthenticationDisabledHealthCheckMessage"),
                "#authentication-disabled");
        }
    }
}
