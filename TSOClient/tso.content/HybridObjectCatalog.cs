using FSO.Content.Interfaces;
using FSO.Content.TS1;
using System.Collections.Generic;

namespace FSO.Content
{
    /// <summary>
    /// Merges TSO WorldObjectCatalog + TS1 TS1ObjectProvider (which implements IObjectCatalog)
    /// into a single catalog. TSO items take priority for GUID collisions.
    ///
    /// TS1ObjectProvider stores objects in "Simitone categories" (0-7 buy, 8-15 build).
    /// This catalog remaps them to TSO UI categories before exposing them:
    ///   TS1 buy  0-7  (Math.Log(FunctionFlags,2))  → TSO buy  12-19
    ///   TS1 build 8-15 (BuildModeType + 7)          → TSO build 0-10 via lookup
    /// </summary>
    public class HybridObjectCatalog : IObjectCatalog
    {
        private WorldObjectCatalog TSOCatalog;
        private TS1ObjectProvider TS1Catalog;

        // Maps TS1 build category (8..15, i.e. BuildModeType 1..8) to TSO build category.
        // TSO UIBuildMode: 0=Doors, 1=Windows, 2=Stairs, 3=Plants, 4=Fireplaces, ..., 29=Debug (admin)
        // TS1 BuildModeType: 1=Doors, 2=Windows, 3=Stairs, 4=Fireplaces, 5=Plants(?), rest → 29 (debug)
        private static readonly sbyte[] TS1BuildTypeToTSOCat = { 0, 1, 2, 4, 3, 29, 29, 29 };

        public HybridObjectCatalog(WorldObjectCatalog tsoCatalog, TS1ObjectProvider ts1Catalog)
        {
            TSOCatalog = tsoCatalog;
            TS1Catalog = ts1Catalog;
        }

        /// <summary>Converts a TS1ObjectProvider category number to the corresponding TSO UI category.</summary>
        private sbyte TS1ToTSOCategory(sbyte ts1Cat)
        {
            if (ts1Cat >= 0 && ts1Cat <= 7)
                return (sbyte)(ts1Cat + 12); // buy mode: 0-7 → 12-19
            if (ts1Cat >= 8 && ts1Cat <= 15)
                return TS1BuildTypeToTSOCat[ts1Cat - 8]; // BuildModeType 1-8 → index 0-7
            return 29; // unrecognised → debug category (admin-only)
        }

        /// <summary>Returns all TS1 category numbers that map to the given TSO category.</summary>
        private IEnumerable<sbyte> TSOToTS1Categories(sbyte tsoCat)
        {
            // Buy mode reverse: TSO 12-19 → TS1 0-7
            if (tsoCat >= 12 && tsoCat <= 19)
            {
                yield return (sbyte)(tsoCat - 12);
                yield break;
            }
            // Build mode reverse: scan the lookup table
            for (int i = 0; i < TS1BuildTypeToTSOCat.Length; i++)
            {
                if (TS1BuildTypeToTSOCat[i] == tsoCat)
                    yield return (sbyte)(i + 8);
            }
        }

        private ObjectCatalogItem RemapItem(ObjectCatalogItem item)
        {
            item.Category = TS1ToTSOCategory(item.Category);
            return item;
        }

        public List<ObjectCatalogItem> All()
        {
            var result = new List<ObjectCatalogItem>(TSOCatalog.All());
            var tsoGuids = new HashSet<uint>();
            foreach (var item in result) tsoGuids.Add(item.GUID);

            foreach (var item in TS1Catalog.All())
            {
                if (!tsoGuids.Contains(item.GUID))
                    result.Add(RemapItem(item));
            }
            return result;
        }

        public List<ObjectCatalogItem> GetItemsByCategory(sbyte category)
        {
            var result = new List<ObjectCatalogItem>(TSOCatalog.GetItemsByCategory(category));
            var tsoGuids = new HashSet<uint>();
            foreach (var item in result) tsoGuids.Add(item.GUID);

            foreach (var ts1Cat in TSOToTS1Categories(category))
            {
                foreach (var item in TS1Catalog.GetItemsByCategory(ts1Cat))
                {
                    if (!tsoGuids.Contains(item.GUID))
                        result.Add(RemapItem(item));
                }
            }
            return result;
        }

        public ObjectCatalogItem? GetItemByGUID(uint guid)
        {
            var item = TSOCatalog.GetItemByGUID(guid);
            if (item != null) return item;
            var ts1Item = TS1Catalog.GetItemByGUID(guid);
            if (ts1Item == null) return null;
            return RemapItem(ts1Item.Value);
        }

        public List<uint> GetUntradableGUIDs()
        {
            var result = new List<uint>(TSOCatalog.GetUntradableGUIDs());
            result.AddRange(TS1Catalog.GetUntradableGUIDs());
            return result;
        }
    }
}
