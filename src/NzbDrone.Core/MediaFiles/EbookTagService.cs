using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Calibre;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles.Azw;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.EbookMetadata;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;
using PdfSharpCore.Pdf.IO;
using VersOne.Epub;
using VersOne.Epub.Schema;

namespace NzbDrone.Core.MediaFiles
{
    public interface IEBookTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        void WriteTags(BookFile trackfile, bool newDownload, bool force = false);
        void SyncTags(List<Edition> books);
        List<RetagBookFilePreview> GetRetagPreviewsByAuthor(int authorId);
        List<RetagBookFilePreview> GetRetagPreviewsByBook(int bookId);
        void RetagFiles(RetagFilesCommand message);
        void RetagAuthor(RetagAuthorCommand message);
    }

    public class EBookTagService : IEBookTagService
    {
        private readonly IAuthorService _authorService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IConfigService _configService;
        private readonly ICalibreProxy _calibre;
        private readonly IEbookFileMetadataWriter _ebookFileMetadataWriter;
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public EBookTagService(IAuthorService authorService,
            IMediaFileService mediaFileService,
            IRootFolderService rootFolderService,
            IConfigService configService,
            ICalibreProxy calibre,
            IEbookFileMetadataWriter ebookFileMetadataWriter,
            IMapCoversToLocal mediaCoverService,
            IDiskProvider diskProvider,
            IRootFolderWatchingService rootFolderWatchingService,
            IEventAggregator eventAggregator,
            Logger logger)
        {
            _authorService = authorService;
            _mediaFileService = mediaFileService;
            _rootFolderService = rootFolderService;
            _configService = configService;
            _calibre = calibre;
            _ebookFileMetadataWriter = ebookFileMetadataWriter;
            _mediaCoverService = mediaCoverService;
            _diskProvider = diskProvider;
            _rootFolderWatchingService = rootFolderWatchingService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
            var extension = file.Extension.ToLower();
            _logger.Trace($"Got extension '{extension}'");

            switch (extension)
            {
                case ".pdf":
                    return ReadPdf(file.FullName);
                case ".epub":
                case ".kepub":
                    return ReadEpub(file.FullName);
                case ".azw3":
                case ".mobi":
                    return ReadAzw3(file.FullName);
                default:
                    return Parser.Parser.ParseTitle(file.FullName);
            }
        }

        public void WriteTags(BookFile bookFile, bool newDownload, bool force = false)
        {
            if (!force)
            {
                if (_configService.WriteBookTags == WriteBookTagsType.NewFiles && !newDownload)
                {
                    return;
                }
            }

            _logger.Debug($"Writing tags for {bookFile}");

            WriteTagsInternal(bookFile, _configService.UpdateCovers, _configService.EmbedMetadata);
        }

        public void SyncTags(List<Edition> editions)
        {
            if (_configService.WriteBookTags != WriteBookTagsType.Sync)
            {
                return;
            }

            // get the tracks to update
            foreach (var edition in editions)
            {
                var bookFiles = edition.BookFiles.Value;

                _logger.Debug($"Syncing ebook tags for {edition}");

                foreach (var file in bookFiles)
                {
                    // populate tracks (which should also have release/book/author set) because
                    // not all of the updates will have been committed to the database yet
                    file.Edition = edition;

                    WriteTagsInternal(file, _configService.UpdateCovers, _configService.EmbedMetadata);
                }
            }
        }

        public List<RetagBookFilePreview> GetRetagPreviewsByAuthor(int authorId)
        {
            var files = _mediaFileService.GetFilesByAuthor(authorId);

            return GetPreviews(files).ToList();
        }

        public List<RetagBookFilePreview> GetRetagPreviewsByBook(int bookId)
        {
            var files = _mediaFileService.GetFilesByBook(bookId);

            return GetPreviews(files).ToList();
        }

        public void RetagFiles(RetagFilesCommand message)
        {
            var author = _authorService.GetAuthor(message.AuthorId);
            var files = _mediaFileService.Get(message.Files);

            _logger.ProgressInfo("Re-tagging {0} ebook files for {1}", files.Count, author.Name);

            foreach (var file in files)
            {
                WriteTagsInternal(file, message.UpdateCovers, message.EmbedMetadata);
            }

            _logger.ProgressInfo("Selected ebook files re-tagged for {0}", author.Name);
        }

        public void RetagAuthor(RetagAuthorCommand message)
        {
            _logger.Debug("Re-tagging all ebook files for selected authors");
            var authorsToRename = _authorService.GetAuthors(message.AuthorIds);

            foreach (var author in authorsToRename)
            {
                var files = _mediaFileService.GetFilesByAuthor(author.Id);

                _logger.ProgressInfo("Re-tagging all ebook files for author: {0}", author.Name);

                foreach (var file in files)
                {
                    WriteTagsInternal(file, message.UpdateCovers, message.EmbedMetadata);
                }

                _logger.ProgressInfo("All ebook files re-tagged for {0}", author.Name);
            }
        }

        private void WriteTagsInternal(BookFile file, bool updateCover, bool embedMetadata)
        {
            var rootFolder = _rootFolderService.GetBestRootFolder(file.Path);

            if (file.CalibreId != 0 && rootFolder != null && rootFolder.IsCalibreLibrary && rootFolder.CalibreSettings != null)
            {
                _calibre.SetFields(file, rootFolder.CalibreSettings, updateCover, embedMetadata);
                return;
            }

            if (!_ebookFileMetadataWriter.CanWrite(file.Path))
            {
                _logger.Trace("No local metadata writer for {0}", file.Path);
                return;
            }

            var metadata = BuildFileMetadata(file, updateCover);
            var changes = new Dictionary<string, Tuple<string, string>>();

            if (_diskProvider.FileExists(file.Path))
            {
                try
                {
                    changes = DiffMetadata(ReadTags(_diskProvider.GetFileInfo(file.Path)), metadata);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Could not read existing tags from {0}; rewriting", file.Path);
                    changes["File"] = Tuple.Create("unreadable", metadata.Title);
                }
            }
            else
            {
                changes["File"] = Tuple.Create((string)null, metadata.Title);
            }

            var coverPending = updateCover && metadata.Cover != null && metadata.Cover.Length > 0;
            if (!changes.Any() && !coverPending)
            {
                _logger.Debug("No tags update for {0} due to no difference", file.Path);
                return;
            }

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(file.Path);
            _ebookFileMetadataWriter.Write(file.Path, metadata, updateCover);

            var fileInfo = _diskProvider.GetFileInfo(file.Path);
            file.Size = fileInfo.Length;
            file.Modified = fileInfo.LastWriteTimeUtc;

            if (file.Id > 0)
            {
                _mediaFileService.Update(file);
            }

            _eventAggregator.PublishEvent(new BookFileRetaggedEvent(file.Author.Value, file, changes, false));
        }

        private EbookFileMetadata BuildFileMetadata(BookFile file, bool updateCover)
        {
            var edition = file.Edition.Value;
            var book = edition.Book.Value;
            var seriesLink = book.SeriesLinks?.Value?.OrderBy(x => x.SeriesPosition)
                .FirstOrDefault(x => x.Series.Value.Title.IsNotNullOrWhiteSpace());

            byte[] cover = null;
            var coverExtension = ".jpg";

            if (updateCover)
            {
                var image = edition.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Cover);
                if (image != null)
                {
                    var coverPath = _mediaCoverService.GetCoverPath(book.Id, MediaCoverEntity.Book, image.CoverType, image.Extension, null);
                    if (_diskProvider.FileExists(coverPath))
                    {
                        cover = File.ReadAllBytes(coverPath);
                        coverExtension = Path.GetExtension(coverPath);
                    }
                }
            }

            return new EbookFileMetadata
            {
                Title = edition.Title,
                Authors = new List<string> { file.Author.Value.Name },
                Publisher = edition.Publisher,
                Language = edition.Language,
                Description = edition.Overview,
                Isbn = edition.Isbn13,
                Asin = edition.Asin,
                Series = seriesLink?.Series.Value.Title,
                SeriesIndex = seriesLink?.Position,
                ReleaseDate = book.ReleaseDate ?? edition.ReleaseDate,
                Cover = cover,
                CoverExtension = coverExtension
            };
        }

        private IEnumerable<RetagBookFilePreview> GetPreviews(List<BookFile> files)
        {
            var calibrePairs = files
                .Where(x => x.CalibreId > 0)
                .Select(x => Tuple.Create(x, _rootFolderService.GetBestRootFolder(x.Path)))
                .Where(x => x.Item2 != null && x.Item2.IsCalibreLibrary && x.Item2.CalibreSettings != null)
                .ToList();

            var calibreFileIds = new HashSet<int>(calibrePairs.Select(x => x.Item1.Id));

            foreach (var preview in GetLocalPreviews(files.Where(f => !calibreFileIds.Contains(f.Id)).ToList()))
            {
                yield return preview;
            }

            var calibreFiles = calibrePairs.Select(x => x.Item1).OrderBy(x => x.Edition.Value.Title).ToList();

            var rootFolderGroups = calibrePairs.GroupBy(x => x.Item2.Path);

            var calibreBooks = new List<CalibreBook>();
            foreach (var group in rootFolderGroups)
            {
                var rootFolder = group.First().Item2;
                var books = _calibre.GetBooks(group.Select(x => x.Item1.CalibreId).ToList(), rootFolder.CalibreSettings);
                calibreBooks.AddRange(books);
            }

            var dict = calibreBooks.ToDictionary(x => x.Id);

            foreach (var file in calibreFiles)
            {
                var edition = file.Edition.Value;
                var book = edition.Book.Value;
                var serieslink = book.SeriesLinks.Value.OrderBy(x => x.SeriesPosition).FirstOrDefault(x => x.Series.Value.Title.IsNotNullOrWhiteSpace());

                var series = serieslink?.Series.Value;
                double? seriesIndex = null;
                if (double.TryParse(serieslink?.Position, out var index))
                {
                    _logger.Trace($"Parsed {serieslink?.Position} as {index}");
                    seriesIndex = index;
                }

                if (!dict.TryGetValue(file.CalibreId, out var oldTags))
                {
                    continue;
                }

                var textInfo = CultureInfo.InvariantCulture.TextInfo;
                var genres = book.Genres.Select(x => textInfo.ToTitleCase(x.Replace('-', ' '))).ToList();

                var newTags = new CalibreBook
                {
                    Title = edition.Title,
                    Authors = new List<string> { file.Author.Value.Name },
                    PubDate = book.ReleaseDate,
                    Publisher = edition.Publisher,
                    Languages = new List<string> { edition.Language.CanonicalizeLanguage() },
                    Tags = genres,
                    Comments = edition.Overview,
                    Rating = (int)(edition.Ratings.Value * 2) / 2.0,
                    Identifiers = new Dictionary<string, string>
                    {
                        { "isbn", edition.Isbn13 },
                        { "asin", edition.Asin },
                        { "goodreads", edition.ForeignEditionId }
                    },
                    Series = series?.Title,
                    Position = seriesIndex
                };

                var diff = oldTags.Diff(newTags);

                if (diff.Any())
                {
                    yield return new RetagBookFilePreview
                    {
                        AuthorId = file.Author.Value.Id,
                        BookId = file.Edition.Value.Id,
                        BookFileId = file.Id,
                        Path = file.Path,
                        Changes = diff
                    };
                }
            }
        }

        private IEnumerable<RetagBookFilePreview> GetLocalPreviews(List<BookFile> files)
        {
            foreach (var file in files.Where(x => _ebookFileMetadataWriter.CanWrite(x.Path)))
            {
                ParsedTrackInfo current;
                try
                {
                    current = ReadTags(_diskProvider.GetFileInfo(file.Path));
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Could not read tags from {0}", file.Path);
                    continue;
                }

                var desired = BuildFileMetadata(file, false);
                var changes = DiffMetadata(current, desired);

                if (changes.Any())
                {
                    yield return new RetagBookFilePreview
                    {
                        AuthorId = file.Author.Value.Id,
                        BookId = file.Edition.Value.Id,
                        BookFileId = file.Id,
                        Path = file.Path,
                        Changes = changes
                    };
                }
            }
        }

        internal static Dictionary<string, Tuple<string, string>> DiffMetadata(ParsedTrackInfo current, EbookFileMetadata desired)
        {
            var changes = new Dictionary<string, Tuple<string, string>>();
            if (current == null || desired == null)
            {
                return changes;
            }

            AddChange(changes, "Title", current.BookTitle, desired.Title);
            var currentAuthors = current.Authors != null && current.Authors.Any() ? string.Join(" / ", current.Authors) : null;
            var desiredAuthors = desired.Authors != null && desired.Authors.Any() ? string.Join(" / ", desired.Authors) : null;
            AddChange(changes, "Author", currentAuthors, desiredAuthors);
            AddChange(changes, "Publisher", current.Publisher, desired.Publisher);
            AddChange(changes, "Isbn", current.Isbn, desired.Isbn);
            AddChange(changes, "Asin", current.Asin, desired.Asin);
            AddChange(changes, "Series", current.SeriesTitle, desired.Series);
            return changes;
        }

        private static void AddChange(Dictionary<string, Tuple<string, string>> changes, string name, string oldValue, string newValue)
        {
            if (!string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
            {
                changes[name] = Tuple.Create(oldValue, newValue);
            }
        }

        private ParsedTrackInfo ReadEpub(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo
            {
                Quality = new QualityModel
                {
                    Quality = Quality.EPUB,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                }
            };

            try
            {
                using (var bookRef = EpubReader.OpenBook(file))
                {
                    result.Authors = bookRef.AuthorList;
                    result.BookTitle = bookRef.Title;

                    var meta = bookRef.Schema.Package.Metadata;

                    _logger.Trace(meta.ToJson());

                    result.Isbn = GetIsbn(meta?.Identifiers);
                    result.Asin = meta?.Identifiers?.FirstOrDefault(x => x.Scheme?.ToLower().Contains("asin") ?? false)?.Identifier;
                    result.Language = meta?.Languages?.FirstOrDefault();
                    result.Publisher = meta?.Publishers?.FirstOrDefault();
                    result.Disambiguation = meta?.Description;

                    result.SeriesTitle = meta?.MetaItems?.FirstOrDefault(x => x.Name == "calibre:series")?.Content;
                    result.SeriesIndex = meta?.MetaItems?.FirstOrDefault(x => x.Name == "calibre:series_index")?.Content;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading epub");
                result.Quality.QualityDetectionSource = QualityDetectionSource.Extension;
            }

            _logger.Trace($"Got:\n{result.ToJson()}");

            return result;
        }

        private ParsedTrackInfo ReadAzw3(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo();

            try
            {
                var book = new Azw3File(file);
                result.Authors = book.Authors;
                result.BookTitle = book.Title;
                result.Isbn = StripIsbn(book.Isbn);
                result.Asin = book.Asin;
                result.Language = book.Language;
                result.Disambiguation = book.Description;
                result.Publisher = book.Publisher;
                result.Label = book.Imprint;
                result.Source = book.Source;

                result.Quality = new QualityModel
                {
                    Quality = book.Version <= 6 ? Quality.MOBI : Quality.AZW3,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading file");

                result.Quality = new QualityModel
                {
                    Quality = Path.GetExtension(file) == ".mobi" ? Quality.MOBI : Quality.AZW3,
                    QualityDetectionSource = QualityDetectionSource.Extension
                };
            }

            _logger.Trace($"Got {result.ToJson()}");

            return result;
        }

        private ParsedTrackInfo ReadPdf(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo
            {
                Quality = new QualityModel
                {
                    Quality = Quality.PDF,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                }
            };

            try
            {
                var book = PdfReader.Open(file, PdfDocumentOpenMode.InformationOnly);
                if (book.Info != null)
                {
                    result.Authors = new List<string> { book.Info.Author };
                    result.BookTitle = book.Info.Title;

                    _logger.Trace(book.Info.ToJson());
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading pdf");
                result.Quality.QualityDetectionSource = QualityDetectionSource.Extension;
            }

            _logger.Trace($"Got:\n{result.ToJson()}");

            return result;
        }

        public string GetIsbn(IEnumerable<EpubMetadataIdentifier> ids)
        {
            var candidates = ids.Select(x => StripIsbn(x?.Identifier))
                .Where(x => x != null)
                .OrderByDescending(x => x.Length);

            return candidates.FirstOrDefault(x => x.StartsWith("978"))
                ?? candidates.FirstOrDefault(x => x.StartsWith("979"))
                ?? candidates.FirstOrDefault();
        }

        private string GetIsbnChars(string input)
        {
            if (input == null)
            {
                return null;
            }

            return new string(input.Where(c => char.IsDigit(c) || c == 'X' || c == 'x').ToArray());
        }

        private string StripIsbn(string input)
        {
            var isbn = GetIsbnChars(input);

            if (isbn == null)
            {
                return null;
            }
            else if ((isbn.Length == 10 && ValidateIsbn10(isbn)) ||
                (isbn.Length == 13 && ValidateIsbn13(isbn)))
            {
                return isbn;
            }

            return null;
        }

        private static char Isbn10Checksum(string isbn)
        {
            var sum = 0;
            for (var i = 0; i < 9; i++)
            {
                sum += int.Parse(isbn[i].ToString()) * (10 - i);
            }

            var result = sum % 11;

            if (result == 0)
            {
                return '0';
            }
            else if (result == 1)
            {
                return 'X';
            }

            return (11 - result).ToString()[0];
        }

        private static char Isbn13Checksum(string isbn)
        {
            var result = 0;
            for (var i = 0; i < 12; i++)
            {
                result += int.Parse(isbn[i].ToString()) * ((i % 2 == 0) ? 1 : 3);
            }

            result %= 10;

            return result == 0 ? '0' : (10 - result).ToString()[0];
        }

        private static bool ValidateIsbn10(string isbn)
        {
            return ulong.TryParse(isbn.Substring(0, 9), out _) && isbn[9] == Isbn10Checksum(isbn);
        }

        private static bool ValidateIsbn13(string isbn)
        {
            return ulong.TryParse(isbn, out _) && isbn[12] == Isbn13Checksum(isbn);
        }
    }
}
