using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Crypto;

namespace NzbDrone.Common.Test
{
    [TestFixture]
    public class HashConverterFixture
    {
        [Test]
        public void get_hash_is_deterministic()
        {
            var first = HashConverter.GetHash("backup-scheduled-readarr.db");
            var second = HashConverter.GetHash("backup-scheduled-readarr.db");

            first.Should().Equal(second);
            HashConverter.GetHashInt31("backup-scheduled-readarr.db")
                .Should()
                .Be(HashConverter.GetHashInt31("backup-scheduled-readarr.db"));
        }

        [Test]
        public void get_hash_int_is_non_negative()
        {
            HashConverter.GetHashInt31("anything").Should().BeGreaterOrEqualTo(0);
        }
    }
}
