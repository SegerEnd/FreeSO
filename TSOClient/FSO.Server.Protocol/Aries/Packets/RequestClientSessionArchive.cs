﻿using Mina.Core.Buffer;
using FSO.Common.Serialization;
using FSO.Common;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class RequestClientSessionArchive : IAriesPacket
    {
        public string ServerKey;
        public ArchiveConfigFlags ArchiveConfig;
        public uint ShardId;
        public string ShardName;
        public string ShardMap;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            ServerKey = input.GetPascalVLCString();
            ArchiveConfig = input.GetEnum<ArchiveConfigFlags>();
            ShardId = input.GetUInt32();
            ShardName = input.GetPascalVLCString();
            ShardMap = input.GetPascalVLCString();
        }

        public AriesPacketType GetPacketType()
        {
            return AriesPacketType.RequestClientSessionArchive;
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalVLCString(ServerKey);
            output.PutEnum(ArchiveConfig);
            output.PutUInt32(ShardId);
            output.PutPascalVLCString(ShardName);
            output.PutPascalVLCString(ShardMap);
        }
    }
}
