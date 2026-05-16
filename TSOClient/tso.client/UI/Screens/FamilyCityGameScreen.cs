using System.IO;
using FSO.Client.Model.FamilyCity;
using FSO.Common;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics;
using FSO.SimAntics.Model.Family;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.Client.UI.Screens
{
    /// <summary>
    /// Top-level screen for playing inside a lot of a <see cref="FamilyCity"/>. Distinct from
    /// <see cref="SandboxGameScreen"/> at the user level (its own LoginScreen entry, its own
    /// future city-overview screen), but reuses the lot-rendering plumbing via inheritance to
    /// stay DRY for now. Refactor to a shared base later if/when needed.
    ///
    /// Phase 2 scope: open a lot for a chosen family. The VM gets <see cref="VMTSOFamilyLotState"/>
    /// pre-set so it can host the family on the TSO platform, and the family is activated before
    /// the first tick via the existing <see cref="VMFamilyComponent.ActivateFamily"/> code path.
    /// </summary>
    public class FamilyCityGameScreen : SandboxGameScreen
    {
        public FamilyCity City { get; private set; }
        public ushort FamilyId { get; private set; }

        public FamilyCityGameScreen() : base() { }

        /// <summary>
        /// Enter the given lot of the given city with the given family active.
        /// If the lot has no <c>.fsov</c> yet, falls back to the default empty blueprint
        /// so the lot can be entered for the first time.
        /// </summary>
        public void EnterCity(FamilyCity city, ushort familyId, int lotId)
        {
            City = city;
            FamilyId = familyId;

            var lotPath = city.GetLotPath(lotId);
            if (!File.Exists(lotPath))
            {
                var defaultBlueprint = Path.Combine(FSOEnvironment.ContentDir, "Blueprints", "empty_lot_fso.xml");
                if (File.Exists(defaultBlueprint))
                    lotPath = defaultBlueprint;
            }
            base.Initialize(lotPath, false);
        }

        protected override void BeforeVMInit(VM vm)
        {
            // Pre-set the family-hosting platform state so VM.Init() adopts it (if-null guard).
            vm.PlatformState = new VMTSOFamilyLotState();
        }

        protected override void AfterVMInit(VM vm)
        {
            if (City == null) return;
            FAMI family = City.Families.GetById(FamilyId);
            if (family == null) return;
            family.SelectWholeFamily();
            ((IVMFamilyLotState)vm.PlatformState).Family.ActivateFamily(vm, family);
        }
    }
}
