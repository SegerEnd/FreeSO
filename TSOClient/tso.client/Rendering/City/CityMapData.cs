using FSO.Content.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Rendering.City
{
    public static class CityMapExtensions
    {
        public static void Save(this CityMap map, string baseDir, CityMapAspects aspects = CityMapAspects.All)
        {
            int w = map.Width;
            int h = map.Height;

            if (aspects.HasFlag(CityMapAspects.Road))
            {
                SaveTex(Path.Combine(baseDir, "roadmap.png"), [.. map.RoadData.Select(x => new Color(x, x, x, (byte)255))], w, h);
            }

            if (aspects.HasFlag(CityMapAspects.Elevation))
            {
                SaveTex(Path.Combine(baseDir, "elevation.png"), [.. map.ElevationData.Select(x => new Color(x, x, x, (byte)255))], w, h);
            }

            if (aspects.HasFlag(CityMapAspects.Forest))
            {
                SaveTex(Path.Combine(baseDir, "forestdensity.png"), [.. map.ForestDensityData.Select(x => new Color(x, x, x, (byte)255))], w, h);
                SaveTex(Path.Combine(baseDir, "foresttype.png"), [.. map.ForestTypeColorData], w, h);
            }

            if (aspects.HasFlag(CityMapAspects.TerrainType))
            {
                SaveTex(Path.Combine(baseDir, "terraintype.png"), [.. map.TerrainTypeColorData], w, h);
            }
        }

        public static void SaveTex(string filename, Color[] data)
        {
            int side = (int)Math.Sqrt(data.Length);
            SaveTex(filename, data, side, side);
        }

        public static void SaveTex(string filename, Color[] data, int width, int height)
        {
            var tex = new Texture2D(GameFacade.GraphicsDevice, width, height);
            tex.SetData(data);
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            Common.Utils.GameThread.NextUpdate(y =>
            {
                var strm = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                tex.SaveAsPng(strm, width, height);
                Common.Utils.GameThread.SetTimeout(() => strm.Close(), 500);
                tex.Dispose();
            });
        }
    }
}
