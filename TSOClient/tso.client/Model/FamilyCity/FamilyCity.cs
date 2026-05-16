using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model.Family;

namespace FSO.Client.Model.FamilyCity
{
    /// <summary>
    /// A single Family City — a local, self-contained TSO-engine world with TS1-style families.
    /// Layout on disk:
    /// <code>
    /// {UserDir}/FamilyCities/{Name}/
    ///   Families.iff         ← FAMI chunks, owned by CityFamilyStore
    ///   Lots/Lot{NN}.fsov    ← per-lot VM snapshots (existing sandbox format)
    ///   Characters/User{NNNNN}.iff ← per-member sims (cloned from TemplatePerson)
    /// </code>
    /// No embedded server, no SQL, no networking — purely files driven by a client-side VM,
    /// in the same spirit as Sandbox but with a multi-lot city layer above it.
    ///
    /// The character iffs are registered in the global <see cref="WorldObjectProvider"/> on
    /// <see cref="LoadCharacters"/> so the VM can spawn them by GUID, and unregistered on
    /// <see cref="UnloadCharacters"/> to avoid polluting the catalog across cities.
    /// </summary>
    public class FamilyCity
    {
        public string Name { get; }
        public string Path { get; }
        public string FamiliesIffPath => System.IO.Path.Combine(Path, "Families.iff");
        public string LotsDirectory => System.IO.Path.Combine(Path, "Lots");
        public string CharactersDirectory => System.IO.Path.Combine(Path, "Characters");

        public CityFamilyStore Families { get; }

        /// <summary>Next free <c>User#####</c> id for character iff filenames.</summary>
        public int NextSim { get; private set; }

        /// <summary>GUIDs of characters this city has registered in the global object catalog.</summary>
        private readonly HashSet<uint> _registeredGuids = new HashSet<uint>();

        public FamilyCity(string name, string path)
        {
            Name = name;
            Path = path;
            Directory.CreateDirectory(LotsDirectory);
            Directory.CreateDirectory(CharactersDirectory);
            Families = new CityFamilyStore(FamiliesIffPath);
        }

        public string GetLotPath(int lotId)
        {
            return System.IO.Path.Combine(LotsDirectory, "Lot" + lotId.ToString().PadLeft(2, '0') + ".fsov");
        }

        public IEnumerable<int> EnumerateLotIds()
        {
            if (!Directory.Exists(LotsDirectory)) yield break;
            foreach (var file in Directory.EnumerateFiles(LotsDirectory, "Lot*.fsov"))
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(file);
                if (name != null && name.Length > 3 && int.TryParse(name.Substring(3), out var id))
                    yield return id;
            }
        }

        /// <summary>
        /// Register every character iff in <see cref="CharactersDirectory"/> with the global
        /// <see cref="WorldObjectProvider"/> so the VM can spawn family members by GUID.
        /// Sets <see cref="NextSim"/> from the highest <c>User#####</c> filename seen.
        /// </summary>
        public void LoadCharacters()
        {
            var worldObjects = Content.Content.Get().WorldObjects;
            if (!Directory.Exists(CharactersDirectory)) return;

            NextSim = 0;
            foreach (var filename in Directory.EnumerateFiles(CharactersDirectory, "*.iff"))
            {
                var basename = System.IO.Path.GetFileNameWithoutExtension(filename);
                if (basename != null && basename.Length > 4 && basename.StartsWith("User")
                    && int.TryParse(basename.Substring(4), out var userId) && userId >= NextSim)
                {
                    NextSim = userId + 1;
                }

                var iff = new IffFile(filename);
                iff.MarkThrowaway();

                var objds = iff.List<OBJD>();
                if (objds == null) continue;
                foreach (var obj in objds)
                {
                    if (worldObjects.Entries.ContainsKey(obj.GUID)) continue; //avoid double-registration
                    //register with the character iff's filename (not the iff's RuntimeInfo path)
                    //so the VM resolves spawns to this file. Same shape TS1NeighborhoodProvider uses.
                    worldObjects.Entries[obj.GUID] = new GameObjectReference(worldObjects)
                    {
                        ID = obj.GUID,
                        FileName = filename,
                        Source = GameObjectSource.User,
                        Name = obj.ChunkLabel,
                        Group = (short)obj.MasterID,
                        SubIndex = obj.SubIndex,
                    };
                    _registeredGuids.Add(obj.GUID);
                }
            }
        }

        /// <summary>
        /// Remove every GUID registered by <see cref="LoadCharacters"/> (or
        /// <see cref="SaveNewCharacter"/>) from the global catalog. Call on city close so a
        /// different city's avatars don't collide later.
        /// </summary>
        public void UnloadCharacters()
        {
            var worldObjects = Content.Content.Get().WorldObjects;
            foreach (var guid in _registeredGuids)
            {
                worldObjects.RemoveObject(guid);
            }
            _registeredGuids.Clear();
        }

        /// <summary>
        /// Save a freshly-cloned TemplatePerson as <c>User#####.iff</c> in this city and
        /// register its OBJD GUID with the global <see cref="WorldObjectProvider"/>.
        /// Mirrors <see cref="FSO.Content.TS1.TS1NeighborhoodProvider.SaveNewNeighbour"/>.
        /// </summary>
        public void SaveNewCharacter(GameObject obj)
        {
            var filename = System.IO.Path.Combine(
                CharactersDirectory,
                "User" + (NextSim++).ToString().PadLeft(5, '0') + ".iff");

            using (var stream = new FileStream(filename, FileMode.Create))
                obj.Resource.MainIff.Write(stream);

            // IMPORTANT: register the entry with the NEW character iff's filename, not the
            // shared TemplatePerson's filename. Mirrors TS1NeighborhoodProvider.SaveNewNeighbour.
            // Using WorldObjectProvider.AddObject(iff, obj) would pick up the template's path
            // and every family member would resolve to the same file (only the first spawns).
            var worldObjects = Content.Content.Get().WorldObjects;
            if (worldObjects.Entries.ContainsKey(obj.OBJ.GUID))
                worldObjects.RemoveObject(obj.OBJ.GUID);
            worldObjects.Entries[obj.OBJ.GUID] = new GameObjectReference(worldObjects)
            {
                ID = obj.OBJ.GUID,
                FileName = filename,
                Source = GameObjectSource.User,
                Name = obj.OBJ.ChunkLabel,
                Group = (short)obj.OBJ.MasterID,
                SubIndex = obj.OBJ.SubIndex,
            };
            _registeredGuids.Add(obj.OBJ.GUID);
        }

        public void Save() => Families.SaveIfDirty();
    }
}
