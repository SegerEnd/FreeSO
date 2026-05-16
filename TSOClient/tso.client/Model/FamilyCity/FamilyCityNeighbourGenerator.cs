using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Utils;

namespace FSO.Client.Model.FamilyCity
{
    /// <summary>
    /// Family City counterpart of <see cref="SimitoneNeighbourGenerator"/>. Same family-creation
    /// flow (clone TemplatePerson per member → generate GUIDs → populate FAMI), scoped to a
    /// <see cref="FamilyCity"/> instead of the TS1 neighbourhood singleton.
    ///
    /// Reuses the existing <see cref="SimTemplateCreateInfo"/> body-string slots so FreeSO's
    /// own TemplatePerson (same GUID as TS1's) gets customized identically — no TS1 content
    /// install required.
    /// </summary>
    public static class FamilyCityNeighbourGenerator
    {
        public static uint TEMPLATE_GUID = 0x7FD96B54;

        /// <summary>
        /// Create a family with the given members in the given city, save everything to disk,
        /// and return the new <see cref="FAMI"/>. After this call the family's GUIDs are
        /// registered in the global object catalog and the city's <c>Families.iff</c> is on disk.
        /// </summary>
        public static FAMI CreateFamily(FamilyCity city, string familyName, SimTemplateCreateInfo[] members)
        {
            if (members == null || members.Length == 0)
                throw new ArgumentException("A family must have at least one member.");

            var fami = CreateFamilyChunk(city, familyName, members.Length);

            for (int i = 0; i < members.Length; i++)
            {
                var guid = fami.FamilyGUIDs[i];
                var info = members[i];
                info.FamilyID = (short)fami.ChunkID;
                PrepareTemplatePerson(city, guid, info);
            }

            city.Save();
            return fami;
        }

        /// <summary>
        /// Clone FreeSO's TemplatePerson (or a custom GUID) into a fresh character iff inside the
        /// given city, mutating its OBJD GUID, label, catalog name, and body strings per the info.
        /// </summary>
        public static void PrepareTemplatePerson(FamilyCity city, uint guid, SimTemplateCreateInfo info)
        {
            var userid = city.NextSim;

            var tempObj = Content.Content.Get().WorldObjects.Get(info.CustomGUID ?? TEMPLATE_GUID);
            tempObj.OBJ.ChunkParent.RetainChunkData = true;
            tempObj.OBJ.GUID = guid;
            tempObj.OBJ.ChunkLabel = "user" + userid.ToString().PadLeft(5, '0') + " - " + info.Name;

            var ctss = tempObj.Resource.Get<CTSS>(2000);
            if (ctss == null)
            {
                ctss = new CTSS()
                {
                    ChunkLabel = "",
                    ChunkID = 2000,
                    ChunkProcessed = true,
                    ChunkType = "CTSS",
                    ChunkParent = tempObj.Resource.MainIff,
                    AddedByPatch = true,
                };
                tempObj.Resource.MainIff.AddChunk(ctss);
                ctss.InsertString(0, new STRItem() { Value = "", Comment = "" });
                ctss.InsertString(0, new STRItem() { Value = "", Comment = "" });
            }
            ctss.SetString(0, info.Name);
            ctss.SetString(1, info.Bio ?? "");
            tempObj.OBJ.CatalogStringsID = 2000;

            var bodyStrings = tempObj.Resource.Get<STR>(200);
            if (bodyStrings != null && info.BodyStringReplace != null)
            {
                foreach (var item in info.BodyStringReplace)
                {
                    bodyStrings.SetString(item.Key, item.Value);
                }
            }

            city.SaveNewCharacter(tempObj);

            // restore the mutated template strings so the next clone starts clean
            if (bodyStrings != null)
            {
                bodyStrings.SetString(1, "");
                bodyStrings.SetString(2, "");
            }
        }

        /// <summary>
        /// Build the FAMI (and matching FAMs name string chunk) for a new family in the city,
        /// generating a fresh ChunkID and a fresh GUID per member slot.
        /// </summary>
        private static FAMI CreateFamilyChunk(FamilyCity city, string familyName, int memberCount)
        {
            // Pick a free ChunkID.
            var existing = city.Families.Families;
            ushort newId = 0;
            var ordered = existing.OrderBy(x => x.ChunkID).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].ChunkID == newId) newId++;
                else break;
            }

            // Generate unique GUIDs for each member slot.
            var guids = new uint[memberCount];
            for (int i = 0; i < memberCount; i++) guids[i] = GenerateGuid(guids);

            var nextFamilyNumber = existing.Count == 0 ? 1 : existing.Max(x => x.FamilyNumber) + 1;

            var fami = new FAMI()
            {
                ChunkLabel = familyName ?? "",
                ChunkID = newId,
                ChunkProcessed = true,
                ChunkType = "FAMI",
                AddedByPatch = true,

                FamilyGUIDs = guids,
                FamilyNumber = nextFamilyNumber,
                Unknown = 24,
                Budget = 20000,
            };
            city.Families.Add(fami);

            // FAMs holds the family's display name as a string chunk keyed by the FAMI ChunkID.
            // Simitone uses this for the in-game family name; keep parity.
            var fams = new FAMs()
            {
                ChunkLabel = "",
                ChunkID = newId,
                ChunkProcessed = true,
                ChunkType = "FAMs",
                AddedByPatch = true,
            };
            fams.InsertString(0, new STRItem() { Comment = "", Value = familyName ?? "" });
            city.Families.Iff.AddChunk(fams);
            city.Families.MarkDirty();

            return fami;
        }

        private static uint GenerateGuid(uint[] avoid)
        {
            var objProvider = Content.Content.Get().WorldObjects;
            lock (objProvider.Entries)
            {
                var rand = new Random();
                var guid = (uint)rand.Next();
                while (objProvider.Entries.ContainsKey(guid) || Array.IndexOf(avoid, guid) >= 0)
                {
                    guid = (uint)rand.Next();
                }
                return guid;
            }
        }
    }
}
