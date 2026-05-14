using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace FSO.Content.UIPacks
{
    /// <summary>
    /// A UI pack — a collection of PNG overrides backed by either a .fsoui/.zip
    /// archive or a loose directory (dev mode).
    ///
    /// Entries are keyed two ways simultaneously:
    ///   * by id   — if the PNG's basename parses as a 16-char hex ulong, it
    ///               overrides a FAR3 UIGraphics asset with that id.
    ///   * by name — otherwise (or in addition), the basename (with extension,
    ///               lowercased) overrides CustomUI assets like
    ///               "credits_fsologo.png" or "archive_logo_1x.png".
    ///
    /// Path within the pack is ignored — only the basename matters, so authors
    /// can organise into subfolders freely.
    /// </summary>
    public class UIPack : IDisposable
    {
        public readonly UIPackManifest Manifest;

        private readonly ZipArchive _archive;
        private readonly string _directory;
        private readonly Dictionary<ulong, string> _entriesById;
        private readonly Dictionary<string, string> _entriesByName;
        private readonly object _lock = new object();

        public IReadOnlyDictionary<ulong, string> EntriesById => _entriesById;
        public IReadOnlyDictionary<string, string> EntriesByName => _entriesByName;

        private UIPack(UIPackManifest manifest, ZipArchive archive, string directory,
                       Dictionary<ulong, string> entriesById, Dictionary<string, string> entriesByName)
        {
            Manifest = manifest;
            _archive = archive;
            _directory = directory;
            _entriesById = entriesById;
            _entriesByName = entriesByName;
        }

        public bool Contains(ulong id) => _entriesById.ContainsKey(id);
        public bool Contains(string name) => _entriesByName.ContainsKey(NormalizeName(name));

        public Stream Open(ulong id) =>
            _entriesById.TryGetValue(id, out var path) ? OpenInternal(path) : null;

        public Stream Open(string name) =>
            _entriesByName.TryGetValue(NormalizeName(name), out var path) ? OpenInternal(path) : null;

        public void Dispose() => _archive?.Dispose();

        public static UIPack Load(string path)
        {
            if (Directory.Exists(path)) return LoadDirectory(path);
            if (File.Exists(path)) return LoadArchive(path);
            return null;
        }

        private Stream OpenInternal(string path)
        {
            lock (_lock)
            {
                if (_archive != null)
                {
                    var entry = _archive.GetEntry(path);
                    if (entry == null) return null;
                    var buffer = new MemoryStream((int)entry.Length);
                    using (var s = entry.Open()) s.CopyTo(buffer);
                    buffer.Position = 0;
                    return buffer;
                }
                return File.OpenRead(Path.Combine(_directory, path));
            }
        }

        private static UIPack LoadArchive(string path)
        {
            var archive = ZipFile.OpenRead(path);
            var manifestEntry = FindEntry(archive, "pack.ini");
            UIPackManifest manifest = null;
            if (manifestEntry != null)
                using (var s = manifestEntry.Open()) manifest = UIPackManifest.Parse(s, path, true);
            manifest ??= DefaultManifest(path, true);

            var byId = new Dictionary<ulong, string>();
            var byName = new Dictionary<string, string>();
            foreach (var entry in archive.Entries)
                IndexEntry(entry.FullName, entry.FullName, byId, byName);
            return new UIPack(manifest, archive, null, byId, byName);
        }

        private static UIPack LoadDirectory(string dir)
        {
            UIPackManifest manifest = null;
            var manifestPath = Path.Combine(dir, "pack.ini");
            if (File.Exists(manifestPath))
            {
                using var s = File.OpenRead(manifestPath);
                manifest = UIPackManifest.Parse(s, dir, false);
            }
            manifest ??= DefaultManifest(dir, false);

            var byId = new Dictionary<ulong, string>();
            var byName = new Dictionary<string, string>();
            foreach (var file in Directory.EnumerateFiles(dir, "*.png", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(dir, file).Replace('\\', '/');
                IndexEntry(relative, relative, byId, byName);
            }
            return new UIPack(manifest, null, dir, byId, byName);
        }

        private static UIPackManifest DefaultManifest(string sourcePath, bool isArchive)
        {
            var fallback = isArchive
                ? Path.GetFileNameWithoutExtension(sourcePath)
                : new DirectoryInfo(sourcePath).Name;
            return new UIPackManifest
            {
                Id = fallback,
                Name = fallback,
                SourcePath = sourcePath,
                IsArchive = isArchive
            };
        }

        /// <summary>
        /// Records the entry under whichever key(s) apply: id if the basename
        /// is hex-shaped, name otherwise. Only PNG entries are indexed.
        /// </summary>
        private static void IndexEntry(string relativePath, string storagePath,
                                       Dictionary<ulong, string> byId, Dictionary<string, string> byName)
        {
            if (!relativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            var basename = Path.GetFileName(relativePath);
            var stem = Path.GetFileNameWithoutExtension(basename);
            var stripped = stem.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? stem.Substring(2) : stem;

            if (ulong.TryParse(stripped, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id)
                && stripped.Length >= 8)
            {
                byId[id] = storagePath;
            }
            else
            {
                byName[NormalizeName(basename)] = storagePath;
            }
        }

        private static ZipArchiveEntry FindEntry(ZipArchive archive, string name)
        {
            foreach (var e in archive.Entries)
                if (string.Equals(e.FullName, name, StringComparison.OrdinalIgnoreCase))
                    return e;
            return null;
        }

        private static string NormalizeName(string name) => name?.ToLowerInvariant();
    }
}
