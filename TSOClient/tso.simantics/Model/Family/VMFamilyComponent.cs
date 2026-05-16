using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Common.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Vitaboy;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.Model.Family
{
    /// <summary>
    /// Holds the active <see cref="FAMI"/> for a lot plus the activation/verification/marshal
    /// helpers that used to live inline on <see cref="TS1Platform.VMTS1LotState"/>.
    ///
    /// One copy of the TS1 family logic for the entire codebase — composed into any platform
    /// state that wants to host families (<see cref="IVMFamilyLotState"/>).
    /// </summary>
    public class VMFamilyComponent
    {
        public FAMI CurrentFamily;

        public void ActivateFamily(VM vm, FAMI family)
        {
            if (family == null) return;
            vm.SetGlobalValue(9, (short)family.ChunkID);
            CurrentFamily = family;
        }

        /// <summary>
        /// Ensure all members of the family are present on the lot.
        /// Spawns missing family members at the mailbox.
        /// </summary>
        public void VerifyFamily(VM vm)
        {
            if (CurrentFamily == null)
            {
                vm.SetGlobalValue(32, 1);
                return;
            }
            vm.SetGlobalValue(32, 0);
            vm.SetGlobalValue(9, (short)CurrentFamily.ChunkID);
            var missingMembers = new HashSet<uint>(CurrentFamily.RuntimeSubset);
            foreach (var avatar in vm.Context.ObjectQueries.Avatars)
            {
                missingMembers.Remove(avatar.Object.OBJ.GUID);
            }

            foreach (var member in missingMembers)
            {
                var sim = vm.Context.CreateObjectInstance(member, LotView.Model.LotTilePos.OUT_OF_WORLD, LotView.Model.Direction.NORTH).Objects[0];
                var avatar = (VMAvatar)sim;
                avatar.SetPersonData(VMPersonDataVariable.TS1FamilyNumber, (short)CurrentFamily.ChunkID);
                var mailbox = vm.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));
                if (mailbox != null) VMFindLocationFor.FindLocationFor(sim, mailbox, vm.Context, VMPlaceRequestFlags.Default);
                avatar.AvatarState.Permissions = VMTSOAvatarPermissions.Owner;

                // On TSO platform (Family City), the cloned TemplatePerson's body strings target TS1
                // mesh names that won't render without TS1 content. Set FreeSO defaults directly so
                // the avatar is visible. Pure TS1/Simitone path (VMTS1LotState) is unchanged.
                if (!vm.TS1)
                {
                    avatar.BodyOutfit = new VMOutfitReference(0x24C0000000DUL);     //default daywear
                    avatar.HeadOutfit = new VMOutfitReference(0x000003a00000000DUL); //bob newbie
                    avatar.SkinTone = AppearanceType.Light;
                }

                vm.Scheduler.RescheduleInterrupt(sim);
            }
        }

        public void SerializeFAMI(BinaryWriter writer)
        {
            writer.Write(CurrentFamily?.ChunkID ?? (ushort)65535);
            if (CurrentFamily != null) CurrentFamily.Write(null, writer.BaseStream);
        }

        public void DeserializeFAMI(BinaryReader reader)
        {
            var famID = reader.ReadUInt16();
            if (famID < 65535)
            {
                CurrentFamily = new FAMI() { ChunkID = famID, ChunkLabel = "", ChunkType = "FAMI" };
                CurrentFamily.Read(null, reader.BaseStream);
            }
            else
            {
                CurrentFamily = null;
            }
        }
    }
}
