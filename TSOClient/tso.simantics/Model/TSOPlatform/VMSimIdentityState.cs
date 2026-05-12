using System.IO;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMSimIdentityState : VMAsyncState
    {
        public bool Success;
        public uint PersistID;
        public string Name = "";
        public ulong BodyOutfit;
        public ulong HeadOutfit;
        public byte SkinTone;
        public short Gender;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Success = reader.ReadBoolean();
            PersistID = reader.ReadUInt32();
            Name = reader.ReadString();
            BodyOutfit = reader.ReadUInt64();
            HeadOutfit = reader.ReadUInt64();
            SkinTone = reader.ReadByte();
            Gender = reader.ReadInt16();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Success);
            writer.Write(PersistID);
            writer.Write(Name ?? "");
            writer.Write(BodyOutfit);
            writer.Write(HeadOutfit);
            writer.Write(SkinTone);
            writer.Write(Gender);
        }
    }
}
