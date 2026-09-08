using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.MediaFiles.EbookMetadata
{
    public class EpubFileMetadataWriter
    {
        private static readonly XNamespace Opf = "http://www.idpf.org/2007/opf";
        private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
        private static readonly XNamespace ContainerNs = "urn:oasis:names:tc:opendocument:xmlns:container";

        public bool CanWrite(string path)
        {
            var extension = Path.GetExtension(path);
            return extension.Equals(".epub", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".kepub", StringComparison.OrdinalIgnoreCase);
        }

        public void Write(string path, EbookFileMetadata metadata, bool updateCover)
        {
            if (path.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Path is required", nameof(path));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("EPUB file not found.", path);
            }

            var tempPath = path + ".tmpwrite";

            try
            {
                using (var sourceStream = File.OpenRead(path))
                using (var source = new ZipArchive(sourceStream, ZipArchiveMode.Read))
                using (var destStream = File.Create(tempPath))
                using (var dest = new ZipArchive(destStream, ZipArchiveMode.Create))
                {
                    var opfPath = GetOpfPath(source);
                    string coverHref = null;

                    if (updateCover && metadata.Cover != null && metadata.Cover.Length > 0)
                    {
                        coverHref = EnsureCoverHref(source, opfPath, metadata.CoverExtension);
                    }

                    foreach (var entry in source.Entries)
                    {
                        if (PathsEqual(entry.FullName, opfPath))
                        {
                            continue;
                        }

                        if (coverHref != null && PathsEqual(entry.FullName, coverHref))
                        {
                            continue;
                        }

                        CopyEntry(entry, dest);
                    }

                    var opfEntry = source.GetEntry(opfPath);
                    if (opfEntry == null)
                    {
                        throw new InvalidDataException("EPUB package document is missing.");
                    }

                    XDocument opfDocument;
                    using (var opfStream = opfEntry.Open())
                    {
                        opfDocument = XDocument.Load(opfStream);
                    }

                    ApplyMetadata(opfDocument, metadata, updateCover, coverHref, opfPath);

                    var newOpf = dest.CreateEntry(opfPath, CompressionLevel.Optimal);
                    using (var outStream = newOpf.Open())
                    {
                        opfDocument.Save(outStream);
                    }

                    if (coverHref != null)
                    {
                        var coverEntry = dest.CreateEntry(coverHref, CompressionLevel.Optimal);
                        using (var coverStream = coverEntry.Open())
                        {
                            coverStream.Write(metadata.Cover, 0, metadata.Cover.Length);
                        }
                    }
                }

                File.Move(tempPath, path, true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        internal static string GetOpfPath(ZipArchive archive)
        {
            var container = archive.GetEntry("META-INF/container.xml");
            if (container == null)
            {
                throw new InvalidDataException("EPUB is missing META-INF/container.xml.");
            }

            using (var stream = container.Open())
            {
                var document = XDocument.Load(stream);
                var fullPath = document.Element(ContainerNs + "container")
                    ?.Element(ContainerNs + "rootfiles")
                    ?.Element(ContainerNs + "rootfile")
                    ?.Attribute("full-path")
                    ?.Value;

                if (fullPath.IsNullOrWhiteSpace())
                {
                    throw new InvalidDataException("EPUB container does not name a package document.");
                }

                return fullPath.Replace('\\', '/');
            }
        }

        internal static void ApplyMetadata(XDocument opfDocument, EbookFileMetadata metadata, bool updateCover, string coverHref, string opfPath)
        {
            var package = opfDocument.Root;
            if (package == null || package.Name.LocalName != "package")
            {
                throw new InvalidDataException("EPUB package document is invalid.");
            }

            var metadataNode = package.Element(Opf + "metadata") ?? package.Element("metadata");
            if (metadataNode == null)
            {
                metadataNode = new XElement(Opf + "metadata");
                package.AddFirst(metadataNode);
            }

            EnsureNamespace(metadataNode, "dc", Dc);
            EnsureNamespace(metadataNode, "opf", Opf);

            SetSingle(metadataNode, Dc + "title", metadata.Title);
            ReplaceCreators(metadataNode, metadata.Authors);
            SetSingle(metadataNode, Dc + "publisher", metadata.Publisher);
            SetSingle(metadataNode, Dc + "language", metadata.Language);
            SetSingle(metadataNode, Dc + "description", metadata.Description);

            if (metadata.ReleaseDate.HasValue)
            {
                SetSingle(metadataNode, Dc + "date", metadata.ReleaseDate.Value.ToString("yyyy-MM-dd"));
            }

            ReplaceManagedIdentifiers(package, metadataNode, metadata);
            ReplaceCalibreSeries(metadataNode, metadata.Series, metadata.SeriesIndex);

            if (updateCover && coverHref != null)
            {
                EnsureCoverManifest(package, metadataNode, coverHref, opfPath, metadata.CoverExtension);
            }
        }

        private static void EnsureNamespace(XElement element, string prefix, XNamespace ns)
        {
            if (element.GetNamespaceOfPrefix(prefix) == null)
            {
                element.SetAttributeValue(XNamespace.Xmlns + prefix, ns.NamespaceName);
            }
        }

        private static void SetSingle(XElement metadataNode, XName name, string value)
        {
            foreach (var existing in metadataNode.Elements(name).ToList())
            {
                existing.Remove();
            }

            if (value.IsNotNullOrWhiteSpace())
            {
                metadataNode.Add(new XElement(name, value));
            }
        }

        private static void ReplaceCreators(XElement metadataNode, List<string> authors)
        {
            foreach (var existing in metadataNode.Elements(Dc + "creator").ToList())
            {
                existing.Remove();
            }

            if (authors == null)
            {
                return;
            }

            foreach (var author in authors.Where(a => a.IsNotNullOrWhiteSpace()))
            {
                var creator = new XElement(Dc + "creator", author);
                creator.SetAttributeValue(Opf + "role", "aut");
                metadataNode.Add(creator);
            }
        }

        private static void ReplaceManagedIdentifiers(XElement package, XElement metadataNode, EbookFileMetadata metadata)
        {
            var uniqueId = package.Attribute("unique-identifier")?.Value;

            foreach (var identifier in metadataNode.Elements(Dc + "identifier").ToList())
            {
                var id = identifier.Attribute("id")?.Value;
                if (uniqueId.IsNotNullOrWhiteSpace() && id == uniqueId)
                {
                    continue;
                }

                var scheme = identifier.Attribute(Opf + "scheme")?.Value
                    ?? identifier.Attribute("scheme")?.Value
                    ?? string.Empty;

                if (scheme.Equals("ISBN", StringComparison.OrdinalIgnoreCase) ||
                    scheme.Equals("ASIN", StringComparison.OrdinalIgnoreCase) ||
                    scheme.IndexOf("isbn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    scheme.IndexOf("asin", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    identifier.Remove();
                }
            }

            AddIdentifier(metadataNode, "ISBN", metadata.Isbn);
            AddIdentifier(metadataNode, "ASIN", metadata.Asin);
        }

        private static void AddIdentifier(XElement metadataNode, string scheme, string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return;
            }

            var element = new XElement(Dc + "identifier", value);
            element.SetAttributeValue(Opf + "scheme", scheme);
            metadataNode.Add(element);
        }

        private static void ReplaceCalibreSeries(XElement metadataNode, string series, string seriesIndex)
        {
            foreach (var meta in metadataNode.Elements(Opf + "meta").Concat(metadataNode.Elements("meta")).ToList())
            {
                var name = meta.Attribute("name")?.Value;
                if (name == "calibre:series" || name == "calibre:series_index")
                {
                    meta.Remove();
                }
            }

            if (series.IsNotNullOrWhiteSpace())
            {
                metadataNode.Add(new XElement(Opf + "meta",
                    new XAttribute("name", "calibre:series"),
                    new XAttribute("content", series)));
            }

            if (seriesIndex.IsNotNullOrWhiteSpace())
            {
                metadataNode.Add(new XElement(Opf + "meta",
                    new XAttribute("name", "calibre:series_index"),
                    new XAttribute("content", seriesIndex)));
            }
        }

        private static string EnsureCoverHref(ZipArchive source, string opfPath, string coverExtension)
        {
            var existing = TryGetExistingCoverHref(source, opfPath);
            if (existing != null)
            {
                return existing;
            }

            var directory = Path.GetDirectoryName(opfPath)?.Replace('\\', '/') ?? string.Empty;
            var fileName = "cover" + NormalizeCoverExtension(coverExtension);
            return string.IsNullOrEmpty(directory) ? fileName : directory.TrimEnd('/') + "/" + fileName;
        }

        private static string TryGetExistingCoverHref(ZipArchive source, string opfPath)
        {
            var opfEntry = source.GetEntry(opfPath);
            if (opfEntry == null)
            {
                return null;
            }

            using (var stream = opfEntry.Open())
            {
                var document = XDocument.Load(stream);
                var package = document.Root;
                var metadataNode = package?.Element(Opf + "metadata") ?? package?.Element("metadata");
                var manifest = package?.Element(Opf + "manifest") ?? package?.Element("manifest");
                var coverId = metadataNode?.Elements().FirstOrDefault(e =>
                    e.Name.LocalName == "meta" && e.Attribute("name")?.Value == "cover")?.Attribute("content")?.Value;

                if (coverId.IsNullOrWhiteSpace() || manifest == null)
                {
                    return null;
                }

                var item = manifest.Elements().FirstOrDefault(e =>
                    e.Name.LocalName == "item" && e.Attribute("id")?.Value == coverId);
                var href = item?.Attribute("href")?.Value;
                if (href.IsNullOrWhiteSpace())
                {
                    return null;
                }

                return CombineOpfHref(opfPath, href);
            }
        }

        private static void EnsureCoverManifest(XElement package, XElement metadataNode, string coverHref, string opfPath, string coverExtension)
        {
            var manifest = package.Element(Opf + "manifest") ?? package.Element("manifest");
            if (manifest == null)
            {
                manifest = new XElement(Opf + "manifest");
                package.Add(manifest);
            }

            const string coverId = "cover-image";
            var relativeHref = MakeRelativeHref(opfPath, coverHref);

            foreach (var meta in metadataNode.Elements().Where(e => e.Name.LocalName == "meta" && e.Attribute("name")?.Value == "cover").ToList())
            {
                meta.Remove();
            }

            metadataNode.Add(new XElement(Opf + "meta",
                new XAttribute("name", "cover"),
                new XAttribute("content", coverId)));

            var existingItem = manifest.Elements().FirstOrDefault(e =>
                e.Name.LocalName == "item" && e.Attribute("id")?.Value == coverId);

            var mediaType = CoverMediaType(coverExtension);
            if (existingItem == null)
            {
                var item = new XElement(Opf + "item");
                item.SetAttributeValue("id", coverId);
                item.SetAttributeValue("href", relativeHref);
                item.SetAttributeValue("media-type", mediaType);
                manifest.Add(item);
            }
            else
            {
                existingItem.SetAttributeValue("href", relativeHref);
                existingItem.SetAttributeValue("media-type", mediaType);
            }
        }

        private static string CombineOpfHref(string opfPath, string href)
        {
            var directory = Path.GetDirectoryName(opfPath)?.Replace('\\', '/') ?? string.Empty;
            var combined = string.IsNullOrEmpty(directory) ? href : directory.TrimEnd('/') + "/" + href.TrimStart('/');
            return combined.Replace('\\', '/');
        }

        private static string MakeRelativeHref(string opfPath, string coverHref)
        {
            var directory = Path.GetDirectoryName(opfPath)?.Replace('\\', '/') ?? string.Empty;
            if (directory.IsNullOrWhiteSpace())
            {
                return coverHref;
            }

            var prefix = directory.TrimEnd('/') + "/";
            if (coverHref.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return coverHref.Substring(prefix.Length);
            }

            return Path.GetFileName(coverHref);
        }

        private static string NormalizeCoverExtension(string extension)
        {
            if (extension.IsNullOrWhiteSpace())
            {
                return ".jpg";
            }

            return extension.StartsWith(".") ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        }

        private static string CoverMediaType(string extension)
        {
            switch (NormalizeCoverExtension(extension))
            {
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                default:
                    return "image/jpeg";
            }
        }

        private static void CopyEntry(ZipArchiveEntry source, ZipArchive dest)
        {
            var level = source.FullName == "mimetype" ? CompressionLevel.NoCompression : CompressionLevel.Optimal;
            var copy = dest.CreateEntry(source.FullName, level);
            using (var input = source.Open())
            using (var output = copy.Open())
            {
                input.CopyTo(output);
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(left?.Replace('\\', '/'), right?.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }
    }
}
