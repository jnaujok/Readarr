using System.IO;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;
using NzbDrone.Test.Common;
using Readarr.Http.Frontend.Mappers;

namespace NzbDrone.Api.Test
{
    [TestFixture]
    public class StaticResourceMapperFixture : TestBase<StaticResourceMapper>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IAppFolderInfo>()
                  .SetupGet(s => s.StartUpFolder)
                  .Returns(TempFolder);

            Mocker.GetMock<IConfigFileProvider>()
                  .SetupGet(s => s.UiFolder)
                  .Returns("UI");
        }

        [Test]
        public void map_returns_path_under_ui_root()
        {
            var mapped = Subject.Map("/Content/Images/logo.png");
            var uiRoot = Path.GetFullPath(Path.Combine(TempFolder, "UI")) + Path.DirectorySeparatorChar;

            mapped.Should().StartWith(uiRoot);
        }

        [Test]
        public void map_rejects_parent_segment_escape()
        {
            Subject.Map("/Content/../../config.xml").Should().BeNull();
        }
    }
}
