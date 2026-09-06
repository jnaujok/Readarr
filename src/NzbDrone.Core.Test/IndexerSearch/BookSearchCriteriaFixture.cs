using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearch
{
    [TestFixture]
    public class BookSearchCriteriaFixture : CoreTest
    {
        [Test]
        public void should_keep_subtitle_after_colon_in_indexer_query()
        {
            var criteria = new BookSearchCriteria
            {
                Author = new Author { Name = "Some Author" },
                BookTitle = "Awaken Online: Armageddon"
            };

            criteria.BookQuery.Should().Contain("Armageddon");
            criteria.BookQuery.Should().Contain("Awaken");
        }

        [Test]
        public void should_strip_author_name_prefix_from_title()
        {
            var criteria = new BookSearchCriteria
            {
                Author = new Author { Name = "Tom Clancy" },
                BookTitle = "Tom Clancy: Ghost Protocol"
            };

            criteria.BookQuery.Should().Contain("Ghost");
            criteria.BookQuery.Should().NotContain("Clancy");
        }
    }
}
