using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Extras;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Extras
{
    [TestFixture]
    public class ExtraFileMatcherFixture : CoreTest
    {
        [TestCase("book.JPG", "jpg,jpeg")]
        [TestCase("cover.jpeg", "jpg, jpeg")]
        [TestCase(@"C:\lib\book.nfo", "nfo")]
        public void should_match_wanted_extensions_case_insensitively(string path, string wanted)
        {
            var extensions = wanted.Split(',').Select(e => e.Trim(' ', '.')).ToArray();
            ExtraFileMatcher.IsWanted(path, extensions).Should().BeTrue();
        }

        [Test]
        public void should_not_match_unlisted_extension()
        {
            ExtraFileMatcher.IsWanted("book.epub", new[] { "jpg", "jpeg" }).Should().BeFalse();
        }
    }
}
