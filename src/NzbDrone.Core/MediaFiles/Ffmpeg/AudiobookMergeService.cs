using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles.Ffmpeg
{
    public interface IAudiobookMergeService
    {
        bool TryMerge(IList<BookFile> files);
    }

    public class AudiobookMergeService : IAudiobookMergeService,
                                         IExecute<MergeAudiobookCommand>,
                                         IHandleAsync<BookImportedEvent>
    {
        private readonly IConfigService _configService;
        private readonly IFfmpegExecutable _ffmpeg;
        private readonly IProcessProvider _processProvider;
        private readonly IDiskProvider _diskProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IAudioTagService _audioTagService;
        private readonly Logger _logger;

        public AudiobookMergeService(IConfigService configService,
                                     IFfmpegExecutable ffmpeg,
                                     IProcessProvider processProvider,
                                     IDiskProvider diskProvider,
                                     IMediaFileService mediaFileService,
                                     IDeleteMediaFiles mediaFileDeletionService,
                                     IBuildFileNames fileNameBuilder,
                                     IRootFolderWatchingService rootFolderWatchingService,
                                     IRootFolderService rootFolderService,
                                     IAudioTagService audioTagService,
                                     Logger logger)
        {
            _configService = configService;
            _ffmpeg = ffmpeg;
            _processProvider = processProvider;
            _diskProvider = diskProvider;
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _fileNameBuilder = fileNameBuilder;
            _rootFolderWatchingService = rootFolderWatchingService;
            _rootFolderService = rootFolderService;
            _audioTagService = audioTagService;
            _logger = logger;
        }

        public void HandleAsync(BookImportedEvent message)
        {
            if (!_configService.MergeAudiobookParts)
            {
                return;
            }

            var imported = message.ImportedBooks ?? new List<BookFile>();
            if (!imported.Any())
            {
                return;
            }

            var editionId = imported[0].EditionId;
            var files = _mediaFileService.GetFilesByEdition(editionId);
            TryMerge(files);
        }

        public void Execute(MergeAudiobookCommand message)
        {
            var files = _mediaFileService.GetFilesByEdition(message.EditionId);
            if (!TryMerge(files))
            {
                throw new Exception("Audiobook merge did not run. Check that FFmpeg is available and there are at least two audio parts.");
            }
        }

        public bool TryMerge(IList<BookFile> files)
        {
            if (!_configService.MergeAudiobookParts)
            {
                return false;
            }

            var parts = (files ?? Array.Empty<BookFile>())
                .Where(f => f != null && MediaFileExtensions.AudioExtensions.Contains(Path.GetExtension(f.Path)))
                .OrderBy(f => f.Part)
                .ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (parts.Count < 2)
            {
                _logger.Debug("Audiobook merge skipped: fewer than two audio parts");
                return false;
            }

            var rootFolder = _rootFolderService.GetBestRootFolder(parts[0].Path);
            if (rootFolder != null && rootFolder.IsCalibreLibrary)
            {
                _logger.Debug("Audiobook merge skipped: Calibre library");
                return false;
            }

            if (!_ffmpeg.IsAvailable())
            {
                _logger.Warn("Audiobook merge skipped: FFmpeg is not available");
                return false;
            }

            var format = _configService.AudiobookMergeFormat;
            var extension = FfmpegMergeArguments.OutputExtension(format);
            var first = parts[0];
            var author = first.Author.Value;
            var edition = first.Edition.Value;
            var namingFile = new BookFile
            {
                Path = first.Path,
                Quality = format == AudiobookMergeFormat.M4b ? new QualityModel(Quality.M4B) : first.Quality,
                Part = 0,
                ReleaseGroup = first.ReleaseGroup
            };

            var fileName = _fileNameBuilder.BuildBookFileName(author, edition, namingFile);
            var outputPath = _fileNameBuilder.BuildBookFilePath(author, edition, fileName, extension);

            if (parts.Any(p => string.Equals(p.Path, outputPath, StringComparison.OrdinalIgnoreCase)))
            {
                outputPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(outputPath) + ".merged" + extension);
            }

            var workDir = Path.Combine(Path.GetTempPath(), "readarr-ffmpeg-" + Guid.NewGuid().ToString("N"));
            _diskProvider.CreateFolder(workDir);

            var concatPath = Path.Combine(workDir, "concat.txt");
            var chaptersPath = Path.Combine(workDir, "chapters.txt");

            try
            {
                _diskProvider.WriteAllText(concatPath, FfmpegConcatList.Build(parts.Select(p => p.Path)));

                var chapters = BuildChapters(parts);
                var hasChapters = chapters.Count == parts.Count;
                if (hasChapters)
                {
                    _diskProvider.WriteAllText(chaptersPath, FfmpegChapterMetadata.Build(chapters));
                }

                var allSameExtension = parts.Select(p => Path.GetExtension(p.Path)).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1;
                var copyCodec = format == AudiobookMergeFormat.Mp3 &&
                                allSameExtension &&
                                Path.GetExtension(parts[0].Path).Equals(".mp3", StringComparison.OrdinalIgnoreCase);

                var args = FfmpegMergeArguments.Build(
                    concatPath,
                    hasChapters ? chaptersPath : null,
                    outputPath,
                    format,
                    copyCodec);

                _rootFolderWatchingService.ReportFileSystemChangeBeginning(outputPath);
                var output = _processProvider.StartAndCapture(_ffmpeg.GetFfmpegPath(), args);

                if (output.ExitCode != 0 || !_diskProvider.FileExists(outputPath))
                {
                    _logger.Warn("FFmpeg merge failed for edition {0} (exit {1})", first.EditionId, output.ExitCode);
                    if (_diskProvider.FileExists(outputPath))
                    {
                        _diskProvider.DeleteFile(outputPath);
                    }

                    return false;
                }

                var merged = new BookFile
                {
                    Path = outputPath,
                    Size = _diskProvider.GetFileSize(outputPath),
                    Modified = _diskProvider.FileGetLastWrite(outputPath),
                    DateAdded = DateTime.UtcNow,
                    Quality = format == AudiobookMergeFormat.M4b ? new QualityModel(Quality.M4B) : first.Quality,
                    EditionId = first.EditionId,
                    CalibreId = 0,
                    Part = 0,
                    ReleaseGroup = first.ReleaseGroup,
                    Author = author,
                    Edition = edition
                };

                foreach (var part in parts)
                {
                    _mediaFileDeletionService.DeleteTrackFile(part);
                }

                _mediaFileService.Add(merged);
                _audioTagService.WriteTags(merged, true, true);

                _logger.ProgressInfo("Merged {0} audiobook parts into {1}", parts.Count, outputPath);
                return true;
            }
            finally
            {
                try
                {
                    if (_diskProvider.FolderExists(workDir))
                    {
                        _diskProvider.DeleteFolder(workDir, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Could not remove FFmpeg work folder {0}", workDir);
                }
            }
        }

        private List<AudiobookChapter> BuildChapters(List<BookFile> parts)
        {
            var chapters = new List<AudiobookChapter>();
            var probe = _ffmpeg.GetFfprobePath();

            foreach (var part in parts)
            {
                double duration = 0;
                try
                {
                    var probeOutput = _processProvider.StartAndCapture(probe, FfmpegMergeArguments.ProbeDurationArgs(part.Path));
                    var text = string.Join("\n", probeOutput.Standard.Select(l => l.Content));
                    FfmpegChapterMetadata.TryParseDurationSeconds(text, out duration);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "ffprobe failed for {0}", part.Path);
                }

                if (duration <= 0)
                {
                    return new List<AudiobookChapter>();
                }

                var title = Path.GetFileNameWithoutExtension(part.Path);
                if (part.Part > 0)
                {
                    title = "Part " + part.Part;
                }

                chapters.Add(new AudiobookChapter { Title = title, DurationSeconds = duration });
            }

            return chapters;
        }
    }
}
