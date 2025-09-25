using FSO.Client;
using FSO.Client.UI.Panels;
using FSO.Common.Rendering.Framework.IO;

#if WINDOWS
using System.Drawing.Imaging;
#endif
using System.Runtime.InteropServices;
#if MACOS
using AppKit;
#endif

namespace FSO.Windows
{
    public static class Program
    {
        public static bool UseDX = true;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
#if MACOS
            // Initialize AppKit for dialogs
            NSApplication.Init();
            NSApplication.SharedApplication.ActivateIgnoringOtherApps(true);
#endif

#if WINDOWS
            InitWindows();

            GameStartProxy.BindClosingHandler = (Func<bool> handler, IntPtr windowHandle) =>
            {
                var form = (Form)Form.FromHandle(windowHandle);
                if (form != null) form.FormClosing += (object sender, FormClosingEventArgs e) =>
                {
                    e.Cancel = !handler();
                };
            };
#endif
            FSOProgram.ShowDialog = ShowDialog;

            if ((new FSOProgram()).InitWithArguments(args))
            {
                var startProxy = new GameStartProxy();
                startProxy.Start(UseDX);
            }
        }

#if WINDOWS
        public static void InitWindows()
        {
            //initialize some platform specific stuff
            FSO.Files.ImageLoaderHelpers.BitmapFunction = BitmapReader;
            ClipboardHandler.Default = new WinFormsClipboard();
            FSO.Files.ImageLoaderHelpers.SavePNGFunc = SavePNG;

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            bool linux = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
            if (!linux) ITTSContext.Provider = UITTSContext.PlatformProvider;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
#endif

        public static void ShowDialog(string text)
        {
#if MACOS
            NSApplication.Init();
            NSApplication.SharedApplication.ActivateIgnoringOtherApps(true);

            var alert = new NSAlert
            {
                MessageText = "This is a message",
                InformativeText = text,
                AlertStyle = NSAlertStyle.Informational
            };
            alert.AddButton("OK");
            alert.RunModal();
#else
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            bool linux = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
            if (linux)
            {
                Console.WriteLine(text);
                Environment.Exit(0);
            }
#endif
#if WINDOWS
            else
            {
                MessageBox.Show(text);
            }
#endif
        }

#if WINDOWS
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject;

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            bool linux = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
            if (linux)
            {
                Console.WriteLine("===== FATAL ERROR =====");
                Console.WriteLine(e.ExceptionObject.ToString());
                Environment.Exit(0);
            }
            else
            {
                if (exception is OutOfMemoryException)
                {
                    MessageBox.Show(e.ExceptionObject.ToString(), "Out of Memory! FreeSO needs to close.");
                }
                else
                {
                    MessageBox.Show(e.ExceptionObject.ToString(), "A fatal error occured! Screenshot this dialog and post it on Discord.");
                }
            }
        }

        public static void SavePNG(byte[] data, int width, int height, Stream str)
        {
            Bitmap image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData bitmapData = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            if (bitmapData.Stride != image.Width * 4)
                throw new NotImplementedException();

            Marshal.Copy(data, 0, bitmapData.Scan0, data.Length);
            image.UnlockBits(bitmapData);

            image.Save(str, ImageFormat.Png);
        }

        public static Tuple<byte[], int, int> BitmapReader(Stream str)
        {
            Bitmap image = (Bitmap)Bitmap.FromStream(str);
            try
            {
                var data = new byte[image.Width * image.Height * 4];

                BitmapData bitmapData = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                if (bitmapData.Stride != image.Width * 4)
                    throw new NotImplementedException();
                Marshal.Copy(bitmapData.Scan0, data, 0, data.Length);
                image.UnlockBits(bitmapData);

                RGBToBGRSoft(data);

                return new Tuple<byte[], int, int>(data, image.Width, image.Height);
            }
            finally
            {
                image.Dispose();
            }
        }

        private const ulong MaskR = 0x000000FF000000FF;
        private const ulong MaskB = 0x00FF000000FF0000;
        private const ulong MaskElse = 0xFF00FF00FF00FF00;

        private unsafe static void RGBToBGRSoft(byte[] data)
        {
            fixed (void* dataPtr = data)
            {
                ulong* longPtr = (ulong*)dataPtr;
                int longCount = data.Length / 8;

                for (int i = 0; i < longCount; i++)
                {
                    ulong px = longPtr[i];
                    longPtr[i] = ((px >> 16) & MaskR) | ((px << 16) & MaskB) | (px & MaskElse);
                }
            }

            if (data.Length % 8 != 0)
            {
                int i = data.Length - 4;
                var temp = data[i];
                data[i] = data[i + 2];
                data[i + 2] = temp;
            }
        }

        // RGB to BGR convert Matrix
        private static float[][] rgbtobgr = new float[][]
        {
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 1}
        };

        internal static Image RGBToBGR(this Image bmp)
        {
            Image newBmp;
            if ((bmp.PixelFormat & System.Drawing.Imaging.PixelFormat.Indexed) != 0)
            {
                newBmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
            else
            {
                newBmp = (Image)bmp.Clone();
            }

            try
            {
                System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes();
                System.Drawing.Imaging.ColorMatrix cm = new System.Drawing.Imaging.ColorMatrix(rgbtobgr);

                ia.SetColorMatrix(cm);
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newBmp))
                {
                    g.Clear(Color.Transparent);
                    g.DrawImage(bmp, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                        0, 0, bmp.Width, bmp.Height, System.Drawing.GraphicsUnit.Pixel, ia);
                }
            }
            finally
            {
                if (newBmp != bmp)
                {
                    bmp.Dispose();
                }
            }

            return newBmp;
        }
#endif
    }
}
