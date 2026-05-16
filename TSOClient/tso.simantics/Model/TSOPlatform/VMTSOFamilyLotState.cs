using System.IO;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model.Family;

namespace FSO.SimAntics.Model.TSOPlatform
{
    /// <summary>
    /// Variant of <see cref="VMTSOLotState"/> that hosts a TS1-style family on top of
    /// the TSO platform (roommates, TSO objects, jobs, etc.). Used by the Archive
    /// (family-enabled flavour) and Sandbox; never by the live online city, which
    /// stays on plain <see cref="VMTSOLotState"/>.
    ///
    /// The marshal extends the base format with a single version byte and an optional
    /// FAMI block, so older .fsov files load cleanly with no family attached.
    /// </summary>
    public class VMTSOFamilyLotState : VMTSOLotState, IVMFamilyLotState
    {
        /// <summary>
        /// Local marshal version for the family extension. Bump if the on-disk family
        /// payload changes shape.
        /// </summary>
        private const byte FAMILY_MARSHAL_VERSION = 1;

        public VMFamilyComponent Family { get; } = new VMFamilyComponent();

        public FAMI CurrentFamily
        {
            get => Family.CurrentFamily;
            set => Family.CurrentFamily = value;
        }

        public VMTSOFamilyLotState() { }
        public VMTSOFamilyLotState(int version) : base(version) { }

        public void ActivateFamily(VM vm, FAMI family) => Family.ActivateFamily(vm, family);

        public void VerifyFamily(VM vm) => Family.VerifyFamily(vm);

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            if (reader.BaseStream.Position >= reader.BaseStream.Length) return;

            var familyVersion = reader.ReadByte();
            if (familyVersion >= 1)
            {
                Family.DeserializeFAMI(reader);
            }
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(FAMILY_MARSHAL_VERSION);
            Family.SerializeFAMI(writer);
        }
    }
}
