using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books.Calibre;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Calibre
{
    [TestFixture]
    public class CalibreGetOriginalFormatFixture : CoreTest
    {
        [Test]
        public void should_skip_formats_with_no_path()
        {
            var formats = new Dictionary<string, CalibreBookFormat>
            {
                { "mobi", new CalibreBookFormat() }
            };

            CalibreProxy.GetOriginalFormat(formats).Should().BeNull();
        }

        [Test]
        public void should_return_null_for_null_formats()
        {
            CalibreProxy.GetOriginalFormat(null).Should().BeNull();
        }

        [Test]
        public void should_return_path_when_present()
        {
            var formats = new Dictionary<string, CalibreBookFormat>
            {
                { "epub", new CalibreBookFormat { Path = "/books/title.epub" } }
            };

            CalibreProxy.GetOriginalFormat(formats).Should().Be("/books/title.epub");
        }
    }
}
