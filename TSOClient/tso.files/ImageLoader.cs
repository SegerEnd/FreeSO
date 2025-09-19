using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using System.Text;

namespace FSO.Files
{
    public struct ImageData
    {
        private Color[] ColorData;
        public Color[] Data => ColorData ?? GetColorData();
        public readonly byte[] ByteData;
        public readonly int Width;
        public readonly int Height;

        public ImageData(Color[] data, int width, int height)
        {
            ColorData = data;
            ByteData = null;
            Width = width;
            Height = height;
        }

        public ImageData(byte[] data, int width, int height)
        {
            ColorData = null;
            ByteData = data;
            Width = width;
            Height = height;
        }

        public Texture2D GetTexture(GraphicsDevice gd)
        {
            if (ColorData == null && ByteData == null) return null;

            var tex = new Texture2D(gd, Width, Height);
            if (ColorData != null)
                tex.SetData(ColorData);
            else
                tex.SetData(ByteData);
            return tex;
        }

        private unsafe Color[] GetColorData()
        {
            var data = ByteData;
            Color[] colorData = new Color[data.Length / 4];
            fixed (void* ptr = colorData)
            {
                Marshal.Copy(data, 0, (IntPtr)ptr, data.Length);
            }
            ColorData = colorData;
            return colorData;
        }
    }

    public readonly struct ImageDataOrTextureProducer
    {
        public readonly ImageData? Data;
        public readonly Func<Texture2D> Producer;

        public ImageDataOrTextureProducer(ImageData data)
        {
            Data = data;
            Producer = null;
        }

        public ImageDataOrTextureProducer(Func<Texture2D> producer)
        {
            Producer = producer;
            Data = null;
        }

        public Texture2D GetTexture(GraphicsDevice gd) => Producer != null ? Producer() : Data?.GetTexture(gd);

        public Func<Texture2D> GetProducer(GraphicsDevice gd)
        {
            if (Producer != null) return Producer;
            if (Data != null)
            {
                var data = Data.Value;
                return () => data.GetTexture(gd);
            }
            return () => null;
        }
    }

    public static class ImageLoader
    {
        public static bool UseSoftLoad = true;
        public static int PremultiplyPNG = 0;

        public static Func<GraphicsDevice, Stream, Texture2D> BaseFunction = WinFromStream;
        public static Func<GraphicsDevice, Stream, Func<Texture2D>> BaseNonUIFunction = WinNonUIFromStream;
        public static Func<GraphicsDevice, Stream, ImageDataOrTextureProducer?> BaseDataFunction = WinDataFromStream;

        public static Texture2D FromStream(GraphicsDevice gd, Stream str) => BaseFunction(gd, str);
        public static ImageDataOrTextureProducer? DataFromStream(GraphicsDevice gd, Stream str) => BaseDataFunction(gd, str);
        public static Func<Texture2D> NonUIFromStream(GraphicsDevice gd, Stream str) => BaseNonUIFunction(gd, str);

        private static Texture2D WinFromStream(GraphicsDevice gd, Stream str) => WinFromStreamP(gd, str, 0);
        private static Func<Texture2D> WinNonUIFromStream(GraphicsDevice gd, Stream str) => WinNonUIFromStreamP(gd, str, 0);
        private static ImageDataOrTextureProducer? WinDataFromStream(GraphicsDevice gd, Stream str) => WinDataFromStreamP(gd, str, 0);

        // Premultiply helpers
        public static bool Premultiply(Color[] buffer, int premult)
        {
            if (premult == 1)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var a = buffer[i].A;
                    if (a != 255) buffer[i] = new Color((byte)((buffer[i].R * a) / 255), (byte)((buffer[i].G * a) / 255), (byte)((buffer[i].B * a) / 255), a);
                }
                return true;
            }
            if (premult == -1)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var a = buffer[i].A / 255f;
                    if (a != 1f) buffer[i] = new Color((byte)(buffer[i].R / a), (byte)(buffer[i].G / a), (byte)(buffer[i].B / a), buffer[i].A);
                }
                return true;
            }
            return false;
        }

        public static bool Premultiply(byte[] buffer, int premult)
        {
            if (premult == 1)
            {
                for (int i = 0; i < buffer.Length; i += 4)
                {
                    var a = buffer[i + 3];
                    if (a != 255)
                    {
                        buffer[i] = (byte)((buffer[i] * a) / 255);
                        buffer[i + 1] = (byte)((buffer[i + 1] * a) / 255);
                        buffer[i + 2] = (byte)((buffer[i + 2] * a) / 255);
                    }
                }
                return true;
            }
            if (premult == -1)
            {
                for (int i = 0; i < buffer.Length; i += 4)
                {
                    var a = buffer[i + 3] / 255f;
                    if (a != 1f)
                    {
                        buffer[i] = (byte)(buffer[i] / a);
                        buffer[i + 1] = (byte)(buffer[i + 1] / a);
                        buffer[i + 2] = (byte)(buffer[i + 2] / a);
                    }
                }
                return true;
            }
            return false;
        }

        // Stream loaders
        public static Func<Texture2D> WinNonUIFromStreamP(GraphicsDevice gd, Stream str, int premult)
        {
            return WinDataFromStreamP(gd, str, premult)?.GetProducer(gd);
        }

        public static ImageDataOrTextureProducer? WinDataFromStreamP(GraphicsDevice gd, Stream str, int premult)
        {
            var magic = (str.ReadByte() | (str.ReadByte() << 8));
            str.Seek(0, SeekOrigin.Begin);

            // BMP
            if (magic == 0x4D42)
            {
                try
                {
                    if (ImageLoaderHelpers.BitmapFunction != null)
                    {
                        var bmp = ImageLoaderHelpers.BitmapFunction(str);
                        if (bmp == null) return null;
                        ManualTextureMaskData(bmp.Item1);
                        return new ImageDataOrTextureProducer(new ImageData(bmp.Item1, bmp.Item2, bmp.Item3));
                    }
                    return new ImageDataOrTextureProducer(() =>
                    {
                        Texture2D tex = Texture2D.FromStream(gd, str);
                        ManualTextureMaskSingleThreaded(ref tex);
                        return tex;
                    });
                }
                catch { return null; }
            }

            // TGA (uncompressed)
            if (IsTga(str))
            {
                try
                {
                    return LoadTga(gd, str);
                }
                catch { return null; }
            }

            // PNG/other
            try
            {
                premult += PremultiplyPNG;
                if (ImageLoaderHelpers.BitmapFunction != null)
                {
                    var bmp = ImageLoaderHelpers.BitmapFunction(str);
                    if (bmp == null) return null;
                    Premultiply(bmp.Item1, premult);
                    return new ImageDataOrTextureProducer(new ImageData(bmp.Item1, bmp.Item2, bmp.Item3));
                }
                return new ImageDataOrTextureProducer(() =>
                {
                    Texture2D tex = Texture2D.FromStream(gd, str);
                    if (premult != 0)
                    {
                        var buffer = new Color[tex.Width * tex.Height];
                        tex.GetData(buffer);
                        Premultiply(buffer, premult);
                        tex.SetData(buffer);
                    }
                    return tex;
                });
            }
            catch
            {
                return new ImageDataOrTextureProducer(new ImageData(new Color[1], 1, 1));
            }
        }

        public static Texture2D WinFromStreamP(GraphicsDevice gd, Stream str, int premult)
        {
            return WinNonUIFromStreamP(gd, str, premult)();
        }

        // Helpers
        private static bool IsTga(Stream str)
        {
            str.Seek(-18, SeekOrigin.End);
            byte[] sig = new byte[16];
            str.Read(sig, 0, 16);
            str.Seek(0, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(sig) == "TRUEVISION-XFILE";
        }

        private static ImageDataOrTextureProducer? LoadTga(GraphicsDevice gd, Stream str)
        {
            using (BinaryReader reader = new BinaryReader(str, Encoding.Default, leaveOpen: true))
            {
                byte idLength = reader.ReadByte();
                byte colorMapType = reader.ReadByte();
                byte imageType = reader.ReadByte();

                if (imageType != 2) return null; // only uncompressed true-color

                reader.BaseStream.Seek(9, SeekOrigin.Current); // skip color map spec
                reader.BaseStream.Seek(4, SeekOrigin.Current); // skip X/Y origin

                ushort width = reader.ReadUInt16();
                ushort height = reader.ReadUInt16();
                byte bpp = reader.ReadByte();
                reader.ReadByte(); // image descriptor

                Color[] pixels = new Color[width * height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte b = reader.ReadByte();
                    byte g = reader.ReadByte();
                    byte r = reader.ReadByte();
                    byte a = (bpp == 32) ? reader.ReadByte() : (byte)255;
                    pixels[i] = new Color(r, g, b, a);
                }

                return new ImageDataOrTextureProducer(new ImageData(pixels, width, height));
            }
        }

        public static bool ManualTextureMaskData(byte[] buffer)
        {
            bool didChange = false;
            for (int i = 0; i < buffer.Length; i += 4)
            {
                if (buffer[i] >= 248 && buffer[i + 2] >= 248 && buffer[i + 1] <= 4)
                {
                    buffer[i] = buffer[i + 1] = buffer[i + 2] = buffer[i + 3] = 0;
                    didChange = true;
                }
            }
            return didChange;
        }

        public static void ManualTextureMaskSingleThreaded(ref Texture2D Texture)
        {
            var size = Texture.Width * Texture.Height * 4;
            byte[] buffer = new byte[size];
            Texture.GetData(buffer);
            if (ManualTextureMaskData(buffer))
            {
                Texture.SetData(buffer);
            }
        }
    }
}
