using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FSO.Common;

namespace FSO.Content.UIPacks
{
    /// <summary>
    /// Discovers UI packs and exposes the currently active one.
    ///
    /// Discovery roots (in order, earlier wins on id collision):
    ///   1. {ContentDir}/UIPacks/*.fsoui|.zip                (archive packs)
    ///   2. {ContentDir}/UIPacks/* /pack.ini                 (loose dev packs)
    ///
    /// Active selection is persisted via the caller (FSOEnvironment / settings),
    /// this class just exposes SetActive(id) / GetActive().
    /// </summary>
    public class UIPackManager
    {
        public const string PacksSubdir = "UIPacks";
        private static readonly string[] ArchiveExtensions = { ".fsoui", ".zip" };

        public readonly UIPackProvider Provider = new UIPackProvider();
        public List<UIPackManifest> AvailablePacks { get; private set; } = new List<UIPackManifest>();
        public event Action<UIPack> ActivePackChanged;

        public string PacksDirectory => Path.Combine(FSOEnvironment.ContentDir, PacksSubdir);

        public void Discover()
        {
            AvailablePacks = new List<UIPackManifest>();
            var dir = PacksDirectory;
            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    if (!IsArchiveExtension(file)) continue;
                    var manifest = TryReadArchiveManifest(file);
                    if (manifest != null) AvailablePacks.Add(manifest);
                }

                foreach (var sub in Directory.EnumerateDirectories(dir))
                {
                    var manifestPath = Path.Combine(sub, "pack.ini");
                    if (!File.Exists(manifestPath)) continue;
                    using var s = File.OpenRead(manifestPath);
                    AvailablePacks.Add(UIPackManifest.Parse(s, sub, false));
                }
            }

            if (!string.IsNullOrEmpty(FSOEnvironment.ActiveUIPack))
                SetActive(FSOEnvironment.ActiveUIPack);
        }

        public bool SetActive(string packId)
        {
            if (string.IsNullOrEmpty(packId))
            {
                Provider.SetActivePack(null);
                ActivePackChanged?.Invoke(null);
                return true;
            }
            var manifest = AvailablePacks.FirstOrDefault(p => string.Equals(p.Id, packId, StringComparison.OrdinalIgnoreCase));
            if (manifest == null) return false;
            var pack = UIPack.Load(manifest.SourcePath);
            if (pack == null) return false;
            Provider.SetActivePack(pack);
            ActivePackChanged?.Invoke(pack);
            return true;
        }

        public string GetActiveId() => Provider.ActivePack?.Manifest?.Id;

        private static bool IsArchiveExtension(string path)
        {
            var ext = Path.GetExtension(path);
            foreach (var allowed in ArchiveExtensions)
                if (string.Equals(ext, allowed, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static UIPackManifest TryReadArchiveManifest(string path)
        {
            try
            {
                using var zip = ZipFile.OpenRead(path);
                foreach (var entry in zip.Entries)
                {
                    if (!string.Equals(entry.FullName, "pack.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    using var s = entry.Open();
                    return UIPackManifest.Parse(s, path, true);
                }
                return new UIPackManifest
                {
                    Id = Path.GetFileNameWithoutExtension(path),
                    Name = Path.GetFileNameWithoutExtension(path),
                    SourcePath = path,
                    IsArchive = true
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
