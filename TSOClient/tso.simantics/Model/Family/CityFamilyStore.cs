using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Model.Family
{
    /// <summary>
    /// City-level container for <see cref="FAMI"/> chunks used by the offline modes
    /// (Archive self-hosted city, Sandbox). Distinct from TS1's <c>Neighborhood.iff</c>:
    /// here the families belong to a TSO city — they reference lot IDs rather than
    /// TS1 house slots, and they live in a flat <c>Families.iff</c> at the city root.
    ///
    /// TS1/Simitone keeps its own <see cref="FSO.Content.TS1.TS1NeighborhoodProvider"/>
    /// and is not affected by this class.
    /// </summary>
    public class CityFamilyStore
    {
        public string FilePath { get; }
        public IffFile Iff { get; private set; }

        private bool _dirty;
        private ushort _nextFamilyId = 1;

        public CityFamilyStore(string filePath)
        {
            FilePath = filePath;
            if (File.Exists(filePath))
            {
                Iff = new IffFile(filePath);
            }
            else
            {
                Iff = new IffFile();
            }

            var existing = Iff.List<FAMI>();
            if (existing != null && existing.Count > 0)
            {
                _nextFamilyId = (ushort)(existing.Max(f => f.ChunkID) + 1);
            }
        }

        public IReadOnlyList<FAMI> Families
        {
            get
            {
                var list = Iff.List<FAMI>();
                return list ?? new List<FAMI>();
            }
        }

        /// <summary>
        /// Adds a new family. Assigns the next free ChunkID if the family doesn't have one,
        /// and registers the chunk with the underlying IFF.
        /// </summary>
        public ushort Add(FAMI family)
        {
            if (family.ChunkID == 0) family.ChunkID = _nextFamilyId++;
            else if (family.ChunkID >= _nextFamilyId) _nextFamilyId = (ushort)(family.ChunkID + 1);

            if (string.IsNullOrEmpty(family.ChunkType)) family.ChunkType = "FAMI";
            if (family.ChunkLabel == null) family.ChunkLabel = "";
            // Programmatically-built chunk: skip the lazy-parse path in IffFile.prepare,
            // which otherwise tries to read a null ChunkData buffer on save.
            family.ChunkProcessed = true;

            Iff.AddChunk(family);
            _dirty = true;
            return family.ChunkID;
        }

        public bool Remove(ushort familyId)
        {
            var fam = Iff.Get<FAMI>(familyId);
            if (fam == null) return false;
            Iff.FullRemoveChunk(fam);
            _dirty = true;
            return true;
        }

        public FAMI GetById(ushort familyId) => Iff.Get<FAMI>(familyId);

        public FAMI GetByLot(int lotId)
        {
            var list = Iff.List<FAMI>();
            if (list == null) return null;
            return list.FirstOrDefault(f => f.HouseNumber == lotId);
        }

        public void MarkDirty() => _dirty = true;

        public bool IsDirty => _dirty;

        /// <summary>
        /// Atomically writes the IFF to disk (temp file + rename) so a crash mid-write
        /// can't corrupt the existing store.
        /// </summary>
        public void Save()
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var tmp = FilePath + ".tmp";
            using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Iff.Write(stream);
            }

            if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
            else File.Move(tmp, FilePath);

            _dirty = false;
        }

        public bool SaveIfDirty()
        {
            if (!_dirty) return false;
            Save();
            return true;
        }
    }
}
