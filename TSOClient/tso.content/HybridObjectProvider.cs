using FSO.Common.Utils;
using FSO.Content.Interfaces;
using FSO.Content.TS1;
using System;
using System.Collections.Generic;

namespace FSO.Content
{
    /// <summary>
    /// Merges TSO WorldObjectProvider + TS1 TS1ObjectProvider into a single AbstractObjectProvider.
    /// TSO takes priority for GUID collisions. Delegates resource loading to the correct
    /// underlying provider based on which one owns each object reference.
    /// </summary>
    public class HybridObjectProvider : AbstractObjectProvider
    {
        private WorldObjectProvider TSOProvider;
        private TS1ObjectProvider TS1Provider;
        private HashSet<ulong> TS1GUIDs;

        public HybridObjectProvider(Content content, WorldObjectProvider tsoProvider, TS1ObjectProvider ts1Provider) : base(content)
        {
            TSOProvider = tsoProvider;
            TS1Provider = ts1Provider;

            TS1GUIDs = new HashSet<ulong>(ts1Provider.Entries.Keys);

            // Merge entries: TSO first, then TS1 (TSO wins on GUID collisions)
            Entries = new Dictionary<ulong, GameObjectReference>(tsoProvider.Entries);
            Cache = new TimedReferenceCache<ulong, GameObject>();
            foreach (var kvp in ts1Provider.Entries)
            {
                if (!Entries.ContainsKey(kvp.Key))
                    Entries[kvp.Key] = kvp.Value;
            }

            ControllerObjects.AddRange(tsoProvider.ControllerObjects);
            ControllerObjects.AddRange(ts1Provider.ControllerObjects);

            foreach (var kvp in tsoProvider.CatalogEnrich)
                CatalogEnrich[kvp.Key] = kvp.Value;
            foreach (var kvp in ts1Provider.CatalogEnrich)
            {
                if (!CatalogEnrich.ContainsKey(kvp.Key))
                    CatalogEnrich[kvp.Key] = kvp.Value;
            }
        }

        public override GameObject Get(ulong id)
        {
            // For TS1-only GUIDs, delegate to TS1Provider which:
            // 1. Correctly sets IsTS1 = true on the returned GameObject
            // 2. Has up-to-date Entries including person GUIDs added by LoadCharacters
            if (!TSOProvider.Entries.ContainsKey(id))
            {
                var ts1Result = TS1Provider.Get(id);
                if (ts1Result != null) return ts1Result;
            }
            return base.Get(id);
        }

        protected override Func<string, GameObjectResource> GenerateResource(GameObjectReference reference)
        {
            if (TS1GUIDs.Contains(reference.ID) && !TSOProvider.Entries.ContainsKey(reference.ID))
            {
                return (fname) =>
                {
                    var obj = TS1Provider.Get(reference.ID);
                    return obj?.Resource;
                };
            }
            else
            {
                return (fname) =>
                {
                    var obj = TSOProvider.Get(reference.ID);
                    return obj?.Resource;
                };
            }
        }
    }
}
