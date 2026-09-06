using Moq;
using NUnit.Framework;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class AuthenticationDisabledCheckFixture : CoreTest<AuthenticationDisabledCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                  .Returns("Authentication is disabled");
        }

        [Test]
        public void should_return_error_when_authentication_is_none()
        {
            Mocker.GetMock<IConfigFileProvider>()
                  .SetupGet(s => s.AuthenticationMethod)
                  .Returns(AuthenticationType.None);

            Subject.Check().ShouldBeError();
        }

        [TestCase(AuthenticationType.Basic)]
        [TestCase(AuthenticationType.Forms)]
        [TestCase(AuthenticationType.External)]
        public void should_return_ok_when_authentication_is_enabled(AuthenticationType method)
        {
            Mocker.GetMock<IConfigFileProvider>()
                  .SetupGet(s => s.AuthenticationMethod)
                  .Returns(method);

            Subject.Check().ShouldBeOk();
        }
    }
}
