using FSO.Content.Interfaces;
using FSO.Content.TS1;
using System.Collections.Generic;

namespace FSO.Content
{
    /// <summary>
    /// Merges TSO WorldObjectCatalog + TS1 TS1ObjectProvider (which implements IObjectCatalog)
    /// into a single catalog. TSO items take priority for GUID collisions.
    /// </summary>
    public class HybridObjectCatalog : IObjectCatalog
    {
        private WorldObjectCatalog TSOCatalog;
        private TS1ObjectProvider TS1Catalog;

        public HybridObjectCatalog(WorldObjectCatalog tsoCatalog, TS1ObjectProvider ts1Catalog)
        {
            TSOCatalog = tsoCatalog;
            TS1Catalog = ts1Catalog;
        }

        public List<ObjectCatalogItem> All()
        {
            var result = new List<ObjectCatalogItem>(TSOCatalog.All());
            var tsoGuids = new HashSet<uint>();
            foreach (var item in result) tsoGuids.Add(item.GUID);

            foreach (var item in TS1Catalog.All())
            {
                if (!tsoGuids.Contains(item.GUID))
                    result.Add(item);
            }
            return result;
        }

        public List<ObjectCatalogItem> GetItemsByCategory(sbyte category)
        {
            var result = new List<ObjectCatalogItem>(TSOCatalog.GetItemsByCategory(category));
            var tsoGuids = new HashSet<uint>();
            foreach (var item in result) tsoGuids.Add(item.GUID);

            foreach (var item in TS1Catalog.GetItemsByCategory(category))
            {
                if (!tsoGuids.Contains(item.GUID))
                    result.Add(item);
            }
            return result;
        }

        public ObjectCatalogItem? GetItemByGUID(uint guid)
        {
            var item = TSOCatalog.GetItemByGUID(guid);
            if (item != null) return item;
            return TS1Catalog.GetItemByGUID(guid);
        }

        public List<uint> GetUntradableGUIDs()
        {
            var result = new List<uint>(TSOCatalog.GetUntradableGUIDs());
            result.AddRange(TS1Catalog.GetUntradableGUIDs());
            return result;
        }
    }
}
