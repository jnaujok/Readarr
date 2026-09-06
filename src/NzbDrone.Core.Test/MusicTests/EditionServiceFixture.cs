using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class EditionServiceFixture : CoreTest<EditionService>
    {
        [Test]
        public void set_monitored_updates_book_file_edition()
        {
            var edition = Builder<Edition>
                .CreateNew()
                .With(e => e.Id = 12)
                .With(e => e.BookId = 5)
                .Build();

            var editions = new List<Edition> { edition };

            Mocker.GetMock<IEditionRepository>()
                  .Setup(s => s.SetMonitored(edition))
                  .Returns(editions);

            Subject.SetMonitored(edition).Should().BeSameAs(editions);

            Mocker.GetMock<IMediaFileRepository>()
                  .Verify(v => v.SetEditionForBook(5, 12), Times.Once());
        }
    }
}
