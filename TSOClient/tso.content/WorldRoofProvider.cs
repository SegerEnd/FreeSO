using FSO.Content.Codecs;
using FSO.Content.Framework;
using FSO.Content.Model;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    public class WorldRoofProvider : FileProvider<ITextureRef>
    {
        public WorldRoofProvider(Content contentManager) : base(contentManager, new TextureCodec(new uint[] { }, true),
            new Regex(Content.TS1Hybrid ? "GameData/Roofs/.*\\.bmp" : "housedata/roofs/.*\\.jpg"))
        {
            UseTS1 = Content.TS1Hybrid;
        }

        public int Count
        {
            get
            {
                return Items.Count;
            }
        }

        public string IDToName(int id)
        {
            return Items[id].Name;
        }

        public int NameToID(string name)
        {
            return Items.FindIndex(x => x.Name == name);
        }
    }
}
