using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Authentication
{
    [TestFixture]
    public class UserServiceFixture : CoreTest<UserService>
    {
        [Test]
        public void add_stores_pbkdf2_hash()
        {
            User stored = null;
            Mocker.GetMock<IUserRepository>()
                  .Setup(s => s.Insert(It.IsAny<User>()))
                  .Returns<User>(u =>
                  {
                      stored = u;
                      return u;
                  });

            Subject.Add("Admin", "secret");

            stored.Password.Should().StartWith(PasswordHasher.Prefix + "$");
            PasswordHasher.Verify("secret", stored.Password).Should().BeTrue();
        }

        [Test]
        public void find_user_upgrades_legacy_hash()
        {
            var user = new User
            {
                Id = 1,
                Identifier = Guid.NewGuid(),
                Username = "admin",
                Password = Hashing.SHA256Hash("secret")
            };

            Mocker.GetMock<IUserRepository>()
                  .Setup(s => s.FindUser("admin"))
                  .Returns(user);

            var result = Subject.FindUser("admin", "secret");

            result.Should().NotBeNull();
            user.Password.Should().StartWith(PasswordHasher.Prefix + "$");
            Mocker.GetMock<IUserRepository>().Verify(v => v.Update(user), Times.Once());
        }

        [Test]
        public void find_user_rejects_wrong_password()
        {
            var user = new User
            {
                Username = "admin",
                Password = PasswordHasher.Hash("secret")
            };

            Mocker.GetMock<IUserRepository>()
                  .Setup(s => s.FindUser("admin"))
                  .Returns(user);

            Subject.FindUser("admin", "wrong").Should().BeNull();
        }
    }
}
