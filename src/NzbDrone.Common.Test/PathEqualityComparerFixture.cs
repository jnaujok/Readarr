using System.Globalization;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test
{
    [TestFixture]
    public class PathEqualityComparerFixture
    {
        [Test]
        public void ignore_case_hash_is_ordinal_not_culture_sensitive()
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var originalUiCulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                var turkish = CultureInfo.GetCultureInfo("tr-TR");
                Thread.CurrentThread.CurrentCulture = turkish;
                Thread.CurrentThread.CurrentUICulture = turkish;

                var upper = PathEqualityComparer.GetHashCode(@"C:\Library\I.epub".AsOsAgnostic(), true);
                var lower = PathEqualityComparer.GetHashCode(@"C:\Library\i.epub".AsOsAgnostic(), true);

                upper.Should().Be(lower);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void instance_hash_matches_os_rules()
        {
            var left = PathEqualityComparer.Instance.GetHashCode(@"C:\Library\Book.epub".AsOsAgnostic());
            var right = PathEqualityComparer.Instance.GetHashCode(@"C:\Library\Book.epub".AsOsAgnostic());

            left.Should().Be(right);

            if (OsInfo.IsWindows)
            {
                PathEqualityComparer.Instance.GetHashCode(@"C:\Library\Book.epub")
                    .Should()
                    .Be(PathEqualityComparer.Instance.GetHashCode(@"c:\library\book.epub"));
            }
        }
    }
}
