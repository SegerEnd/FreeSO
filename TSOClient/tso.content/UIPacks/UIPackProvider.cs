using System.Collections.Generic;
using FSO.Content.Codecs;
using FSO.Content.Model;

namespace FSO.Content.UIPacks
{
    /// <summary>
    /// Returns texture overrides for the currently active UI pack. The active
    /// pack is owned by <see cref="UIPackManager"/>; this class is the thin
    /// query surface consulted by both UIGraphicsProvider (id lookup) and
    /// CustomUIProvider (name lookup).
    ///
    /// Both caches are keyed independently so switching packs only needs
    /// Invalidate().
    /// </summary>
    public class UIPackProvider
    {
        private readonly TextureCodec _codec = new TextureCodec();
        private readonly Dictionary<ulong, ITextureRef> _idCache = new Dictionary<ulong, ITextureRef>();
        private readonly Dictionary<string, ITextureRef> _nameCache = new Dictionary<string, ITextureRef>();
        private readonly object _lock = new object();
        private UIPack _active;

        public UIPack ActivePack
        {
            get { lock (_lock) return _active; }
        }

        public void SetActivePack(UIPack pack)
        {
            lock (_lock)
            {
                if (_active == pack) return;
                _active?.Dispose();
                _active = pack;
                _idCache.Clear();
                _nameCache.Clear();
            }
        }

        public void Invalidate()
        {
            lock (_lock)
            {
                _idCache.Clear();
                _nameCache.Clear();
            }
        }

        /// <summary>
        /// Returns the override texture for <paramref name="id"/>, or null.
        /// </summary>
        public ITextureRef Get(ulong id)
        {
            lock (_lock)
            {
                if (_active == null || !_active.Contains(id)) return null;
                if (_idCache.TryGetValue(id, out var cached)) return cached;
                using var stream = _active.Open(id);
                if (stream == null) return null;
                var tex = _codec.Decode(stream);
                _idCache[id] = tex;
                return tex;
            }
        }

        /// <summary>
        /// Returns the override texture for the asset whose basename matches
        /// <paramref name="name"/> (e.g. "credits_fsologo.png"), or null.
        /// </summary>
        public ITextureRef Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var key = name.ToLowerInvariant();
            lock (_lock)
            {
                if (_active == null || !_active.Contains(key)) return null;
                if (_nameCache.TryGetValue(key, out var cached)) return cached;
                using var stream = _active.Open(key);
                if (stream == null) return null;
                var tex = _codec.Decode(stream);
                _nameCache[key] = tex;
                return tex;
            }
        }
    }
}
