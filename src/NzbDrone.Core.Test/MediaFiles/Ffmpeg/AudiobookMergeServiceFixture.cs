using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Books;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Ffmpeg;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.Ffmpeg
{
    [TestFixture]
    public class AudiobookMergeServiceFixture : CoreTest<AudiobookMergeService>
    {
        private List<BookFile> Files(params string[] paths)
        {
            var author = Builder<Author>.CreateNew().Build();
            var edition = Builder<Edition>.CreateNew().Build();
            var list = new List<BookFile>();
            var part = 1;
            foreach (var path in paths)
            {
                list.Add(Builder<BookFile>.CreateNew()
                    .With(f => f.Path = path)
                    .With(f => f.Part = part++)
                    .With(f => f.Author = author)
                    .With(f => f.Edition = edition)
                    .Build());
            }

            return list;
        }

        [Test]
        public void skips_when_disabled()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.MergeAudiobookParts).Returns(false);
            Subject.TryMerge(Files("/a.mp3", "/b.mp3")).Should().BeFalse();
        }

        [Test]
        public void skips_when_fewer_than_two_audio_files()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.MergeAudiobookParts).Returns(true);
            Subject.TryMerge(Files("/a.mp3")).Should().BeFalse();
        }

        [Test]
        public void skips_when_ffmpeg_missing()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.MergeAudiobookParts).Returns(true);
            Mocker.GetMock<IFfmpegExecutable>().Setup(s => s.IsAvailable()).Returns(false);
            Subject.TryMerge(Files("/a.mp3", "/b.mp3")).Should().BeFalse();
        }

        [Test]
        public void skips_calibre_library()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.MergeAudiobookParts).Returns(true);
            Mocker.GetMock<IRootFolderService>()
                .Setup(s => s.GetBestRootFolder(It.IsAny<string>()))
                .Returns(new RootFolder { IsCalibreLibrary = true });

            Subject.TryMerge(Files("/a.mp3", "/b.mp3")).Should().BeFalse();
            Mocker.GetMock<IFfmpegExecutable>().Verify(v => v.IsAvailable(), Times.Never());
        }

        [Test]
        public void merges_parts_when_ffmpeg_succeeds()
        {
            var files = Files("/lib/a.mp3", "/lib/b.mp3");
            Mocker.GetMock<IConfigService>().SetupGet(c => c.MergeAudiobookParts).Returns(true);
            Mocker.GetMock<IConfigService>().SetupGet(c => c.AudiobookMergeFormat).Returns(AudiobookMergeFormat.M4b);
            Mocker.GetMock<IFfmpegExecutable>().Setup(s => s.IsAvailable()).Returns(true);
            Mocker.GetMock<IFfmpegExecutable>().Setup(s => s.GetFfmpegPath()).Returns("ffmpeg");
            Mocker.GetMock<IFfmpegExecutable>().Setup(s => s.GetFfprobePath()).Returns("ffprobe");
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(It.IsAny<string>())).Returns((RootFolder)null);
            Mocker.GetMock<IBuildFileNames>()
                .Setup(s => s.BuildBookFileName(It.IsAny<Author>(), It.IsAny<Edition>(), It.IsAny<BookFile>(), null, null))
                .Returns("Book");
            Mocker.GetMock<IBuildFileNames>()
                .Setup(s => s.BuildBookFilePath(It.IsAny<Author>(), It.IsAny<Edition>(), "Book", ".m4b"))
                .Returns("/lib/Book.m4b");

            var probe = new ProcessOutput { ExitCode = 0 };
            probe.Lines.Add(new ProcessOutputLine(ProcessOutputLevel.Standard, "10.0"));
            Mocker.GetMock<IProcessProvider>()
                .Setup(s => s.StartAndCapture("ffprobe", It.IsAny<string>(), null))
                .Returns(probe);
            Mocker.GetMock<IProcessProvider>()
                .Setup(s => s.StartAndCapture("ffmpeg", It.IsAny<string>(), null))
                .Returns(new ProcessOutput { ExitCode = 0 });

            Mocker.GetMock<IDiskProvider>().Setup(d => d.FileExists("/lib/Book.m4b")).Returns(true);
            Mocker.GetMock<IDiskProvider>().Setup(d => d.GetFileSize("/lib/Book.m4b")).Returns(1000);
            Mocker.GetMock<IDiskProvider>().Setup(d => d.FileGetLastWrite("/lib/Book.m4b")).Returns(DateTime.UtcNow);

            Subject.TryMerge(files).Should().BeTrue();

            Mocker.GetMock<IDeleteMediaFiles>().Verify(v => v.DeleteTrackFile(It.IsAny<BookFile>(), ""), Times.Exactly(2));
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.Is<BookFile>(f => f.Path == "/lib/Book.m4b" && f.Part == 0)), Times.Once());
        }
    }
}
