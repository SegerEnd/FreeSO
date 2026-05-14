using FSO.Content.Codecs;
using FSO.Content.Framework;
using FSO.Content.Model;
using FSO.Content.UIPacks;
using System.IO;
using System.Text.RegularExpressions;

namespace FSO.Content.TS1
{
    public class CustomUIProvider : FileProvider<ITextureRef>
    {
        /// <summary>
        /// Optional override source consulted before falling back to the loose
        /// PNG on disk. Wired by <see cref="Content"/> at construction time.
        /// </summary>
        public UIPackProvider PackOverride;

        public CustomUIProvider(Content contentManager)
            : base(contentManager, new TextureCodec(), new Regex("uigraphics/.*\\.png"))
        {
            UseContent = true;
        }

        public new ITextureRef Get(string name)
        {
            var packOverride = PackOverride?.Get(Path.GetFileName(name));
            if (packOverride != null) return packOverride;
            return base.Get(name);
        }
    }
}
