using FSO.Files.Utils;
using System.IO.Compression;

namespace FSO.Content.Model
{
    public class CityMapMarshal
    {
        private const int LegacyMapSize = 512;

        private const uint Magic = 0x4D435346;
        private const int Version = 1;

        public int Width = LegacyMapSize;
        public int Height = LegacyMapSize;

        public byte[] TerrainType;
        public byte[] ElevationMap;
        public byte[] RoadMap;

        public byte[] ForestDensity;
        public byte[] ForestType;

        public CityMapMarshal()
        {

        }

        public void Write(Stream str)
        {
            using (var io = IoWriter.FromStream(str))
            {
                io.WriteUInt32(Magic);
                io.WriteInt32(Version);
                io.WriteInt32(Width);
                io.WriteInt32(Height);

                io.WriteBytes(TerrainType);
                io.WriteBytes(ElevationMap);
                io.WriteBytes(RoadMap);
                io.WriteBytes(ForestDensity);
                io.WriteBytes(ForestType);
            }
        }

        public byte[] Write()
        {
            using (var mem = new MemoryStream())
            {
                Write(mem);

                return mem.ToArray();
            }
        }

        public void Read(Stream str)
        {
            using (var io = IoBuffer.FromStream(str))
            {
                var first = io.ReadUInt32();

                if (first == Magic)
                {
                    var version = io.ReadInt32();
                    if (version != Version)
                    {
                        throw new Exception($"Unsupported CityMapMarshal version {version}");
                    }

                    Width = io.ReadInt32();
                    Height = io.ReadInt32();
                }
                else
                {
                    Width = LegacyMapSize;
                    Height = LegacyMapSize;

                    int pixelCount = Width * Height;
                    var rest = io.ReadBytes(pixelCount - 4);
                    TerrainType = new byte[pixelCount];
                    TerrainType[0] = (byte)(first & 0xFF);
                    TerrainType[1] = (byte)((first >> 8) & 0xFF);
                    TerrainType[2] = (byte)((first >> 16) & 0xFF);
                    TerrainType[3] = (byte)((first >> 24) & 0xFF);
                    Array.Copy(rest, 0, TerrainType, 4, rest.Length);

                    ElevationMap = io.ReadBytes(pixelCount);
                    RoadMap = io.ReadBytes(pixelCount);
                    ForestDensity = io.ReadBytes(pixelCount);
                    ForestType = io.ReadBytes(pixelCount);
                    return;
                }

                int count = Width * Height;
                TerrainType = io.ReadBytes(count);
                ElevationMap = io.ReadBytes(count);
                RoadMap = io.ReadBytes(count);
                ForestDensity = io.ReadBytes(count);
                ForestType = io.ReadBytes(count);
            }
        }

        public void Read(byte[] data)
        {
            using (var mem = new MemoryStream(data))
            {
                Read(mem);
            }
        }
    }
}
