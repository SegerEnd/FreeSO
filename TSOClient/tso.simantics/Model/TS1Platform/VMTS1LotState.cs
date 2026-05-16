using FSO.SimAntics.Model.Platform;
using FSO.SimAntics.Model.Family;
using System.IO;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Utils;

namespace FSO.SimAntics.Model.TS1Platform
{
    public class VMTS1LotState : VMAbstractLotState, IVMFamilyLotState
    {
        public SIMI SimulationInfo;

        public VMFamilyComponent Family { get; } = new VMFamilyComponent();

        public FAMI CurrentFamily
        {
            get => Family.CurrentFamily;
            set => Family.CurrentFamily = value;
        }

        public VMTS1LotState() : base() { }
        public VMTS1LotState(int version) : base(version) { }

        public void ActivateFamily(VM vm, FAMI family) => Family.ActivateFamily(vm, family);

        public void VerifyFamily(VM vm) => Family.VerifyFamily(vm);

        public override void Deserialize(BinaryReader reader)
        {
            if (reader.ReadBoolean())
            {
                SimulationInfo = new SIMI() { ChunkID = 1, ChunkLabel = "", ChunkType = "SIMI" };
                SimulationInfo.Read(null, reader.BaseStream);
            }

            //this is really only here for future networking. families should be activated (see abover) when joining lots for the first time
            Family.DeserializeFAMI(reader);
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(SimulationInfo != null);
            SimulationInfo?.Write(null, writer.BaseStream);
            Family.SerializeFAMI(writer);
        }

        public override void Tick(VM vm, object owner)
        {
        }

        public void UpdateSIMI(VM vm)
        {
            if (SimulationInfo == null) return;

            var objValue = VMArchitectureStats.GetObjectValue(vm);
            SimulationInfo.ArchitectureValue = VMArchitectureStats.GetArchValue(vm.Context.Architecture) + objValue.Item2;
            SimulationInfo.ObjectsValue = objValue.Item1;
            SimulationInfo.Version = 0x3E;
            SimulationInfo.GlobalData = vm.GlobalState;
        }

        public override void ActivateValidator(VM vm)
        {
            Validator = new VMDefaultValidator(vm);
        }
    }
}
