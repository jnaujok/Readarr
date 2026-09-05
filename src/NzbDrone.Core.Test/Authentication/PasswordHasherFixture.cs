using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Authentication;

namespace NzbDrone.Core.Test.Authentication
{
    [TestFixture]
    public class PasswordHasherFixture
    {
        [Test]
        public void hash_is_not_sha256_hex_and_verifies()
        {
            var hash = PasswordHasher.Hash("secret");

            hash.Should().StartWith(PasswordHasher.Prefix + "$");
            PasswordHasher.Verify("secret", hash).Should().BeTrue();
            PasswordHasher.Verify("wrong", hash).Should().BeFalse();
            PasswordHasher.NeedsRehash(hash).Should().BeFalse();
        }

        [Test]
        public void verify_accepts_legacy_sha256_and_needs_rehash()
        {
            var legacy = Hashing.SHA256Hash("secret");

            PasswordHasher.Verify("secret", legacy).Should().BeTrue();
            PasswordHasher.Verify("wrong", legacy).Should().BeFalse();
            PasswordHasher.NeedsRehash(legacy).Should().BeTrue();
        }

        [Test]
        public void hashes_are_unique_per_call()
        {
            PasswordHasher.Hash("secret").Should().NotBe(PasswordHasher.Hash("secret"));
        }
    }
}
