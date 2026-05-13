using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace FSO.Common.Domain.Realestate
{
    public class MapCoordinates
    {
        public static int MapWidth = 512;
        public static int MapHeight = 512;

        public static MapCoordinate Offset(ushort x, ushort y, int offsetX, int offsetY)
        {
            return Offset(new MapCoordinate(x, y), offsetX, offsetY);
        }

        public static MapCoordinate Offset(MapCoordinate coord, int offsetX, int offsetY)
        {
            //Tile above = 0, -1
            //Tile below = 0, 1
            //Tile left = -1, 0
            //Tile right = 1, 0
            return new MapCoordinate((ushort)(coord.X - offsetY), (ushort)(coord.Y + offsetX));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InBounds(ushort x, ushort y){
            return InBounds(x, y, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InBounds(ushort x, ushort y, ushort padding)
        {
            if (y < padding) { return false; }
            if (y > (MapHeight - 1 - padding)) { return false; }

            var (xStart, xEnd) = DiamondBounds(y, MapWidth, MapHeight);

            if (x < xStart + padding) { return false; }
            if (x > xEnd - padding) { return false; }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int xStart, int xEnd) DiamondBounds(int y, int mapWidth, int mapHeight)
        {
            int diagTop = mapHeight * 306 / 512;
            int diagBot = mapHeight * 205 / 512;
            int rightTop = mapWidth * 307 / 512;

            int xStart = y < diagTop ? diagTop - y : y - diagTop;
            int xEnd = y < diagBot ? rightTop + y : mapWidth - (y - diagBot);

            return (xStart, xEnd);
        }

        public static Vector2 ClampToDiamond(Vector2 value, int mapWidth)
        {
            var trans = new Vector2((value.X + value.Y) / 2, (value.Y - value.X) / 2);

            float scale = mapWidth / 512f;
            float midX = mapWidth / 2f;
            trans.X = Math.Max(midX - 102.5f * scale, Math.Min(midX + 102.5f * scale, trans.X));
            trans.Y = Math.Max(-152f * scale, Math.Min(152f * scale, trans.Y));

            return new Vector2(trans.X - trans.Y, trans.X + trans.Y);
        }

        public static uint Pack(ushort x, ushort y)
        {
            return (uint)(x << 16 | y);
        }

        public static MapCoordinate Unpack(uint value)
        {
            var x = value >> 16;
            var y = value & 0xFFFF;
            return new MapCoordinate((ushort)x, (ushort)y);
        }
    }

    public struct MapCoordinate
    {
        public MapCoordinate(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        public MapCoordinate(Point point)
        {
            X = (ushort)point.X;
            Y = (ushort)point.Y;
        }

        public ushort X;
        public ushort Y;

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public Point ToPoint()
        {
            return new Point(X, Y);
        }
    }
}
