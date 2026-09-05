using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using NLog;
using NzbDrone.Common.Disk;

namespace NzbDrone.Common
{
    public interface IArchiveService
    {
        void Extract(string compressedFile, string destination);
        void CreateZip(string path, IEnumerable<string> files);
    }

    public class ArchiveService : IArchiveService
    {
        private readonly Logger _logger;

        public ArchiveService(Logger logger)
        {
            _logger = logger;
        }

        public void Extract(string compressedFile, string destination)
        {
            _logger.Debug("Extracting archive [{0}] to [{1}]", compressedFile, destination);

            Directory.CreateDirectory(destination);

            if (compressedFile.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                ExtractZip(compressedFile, destination);
            }
            else
            {
                ExtractTgz(compressedFile, destination);
            }

            _logger.Debug("Extraction complete.");
        }

        public void CreateZip(string path, IEnumerable<string> files)
        {
            _logger.Debug("Creating archive {0}", path);

            using var zipFile = ZipFile.Create(path);

            zipFile.BeginUpdate();

            foreach (var file in files)
            {
                zipFile.Add(file, Path.GetFileName(file));
            }

            zipFile.CommitUpdate();
        }

        private void ExtractZip(string compressedFile, string destination)
        {
            using (var fileStream = File.OpenRead(compressedFile))
            using (var zipFile = new ZipFile(fileStream))
            {
                _logger.Debug("Validating Archive {0}", compressedFile);

                if (!zipFile.TestArchive(true, TestStrategy.FindFirstError, OnZipError))
                {
                    throw new IOException(string.Format("File {0} failed archive validation.", compressedFile));
                }

                foreach (ZipEntry zipEntry in zipFile)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;
                    }

                    var fullZipToPath = GetSafeExtractPath(destination, zipEntry.Name);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    var buffer = new byte[4096];
                    using (var zipStream = zipFile.GetInputStream(zipEntry))
                    using (var streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
            }
        }

        private void ExtractTgz(string compressedFile, string destination)
        {
            using (var inStream = File.OpenRead(compressedFile))
            using (var gzipStream = new GZipInputStream(inStream))
            using (var tarInput = new TarInputStream(gzipStream, Encoding.UTF8))
            {
                TarEntry entry;
                while ((entry = tarInput.GetNextEntry()) != null)
                {
                    if (entry.IsDirectory)
                    {
                        continue;
                    }

                    var fullPath = GetSafeExtractPath(destination, entry.Name);
                    var directoryName = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    using (var outStream = File.Create(fullPath))
                    {
                        tarInput.CopyEntryContents(outStream);
                    }
                }
            }
        }

        private static string GetSafeExtractPath(string destination, string entryName)
        {
            if (string.IsNullOrWhiteSpace(entryName))
            {
                throw new IOException("Archive entry has an empty name.");
            }

            var relative = entryName.Replace('/', Path.DirectorySeparatorChar)
                                    .Replace('\\', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(relative))
            {
                throw new IOException($"Archive entry '{entryName}' is rooted and will not be extracted.");
            }

            var destinationRoot = Path.GetFullPath(destination);
            if (destinationRoot.Length > 0 &&
                destinationRoot[destinationRoot.Length - 1] != Path.DirectorySeparatorChar &&
                destinationRoot[destinationRoot.Length - 1] != Path.AltDirectorySeparatorChar)
            {
                destinationRoot += Path.DirectorySeparatorChar;
            }

            var fullPath = Path.GetFullPath(Path.Combine(destinationRoot, relative));

            if (!fullPath.StartsWith(destinationRoot, DiskProviderBase.PathStringComparison))
            {
                throw new IOException($"Archive entry '{entryName}' would extract outside '{destination}'.");
            }

            return fullPath;
        }

        private void OnZipError(TestStatus status, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _logger.Error("File {0} failed zip validation. {1}", status.File.Name, message);
            }
        }
    }
}
