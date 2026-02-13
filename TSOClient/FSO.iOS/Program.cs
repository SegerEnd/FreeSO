using System;
using System.IO;
using System.Threading;
using Foundation;
using UIKit;
using FSO.Client;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Files;
using Microsoft.Xna.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace FSO.iOS
{
    public static class Program
    {
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }

    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private UIWindow _window;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var tsoPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "The Sims Online/TSOClient/tuning.dat");

            if (File.Exists(tsoPath))
            {
                RunGame();
            }
            else
            {
                _window = new UIWindow(UIScreen.MainScreen.Bounds);
                var viewController = new FSOInstallViewController();
                viewController.OnInstalled += () =>
                {
                    try
                    {
                        RunGame();
                    }
                    catch (Exception ex)
                    {
                        var alert = UIAlertController.Create("Game Error",
                            "Failed to start game: " + ex.ToString(), UIAlertControllerStyle.Alert);
                        alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
                        _window.RootViewController.PresentViewController(alert, true, null);
                    }
                };
                _window.RootViewController = viewController;
                _window.MakeKeyAndVisible();
            }

            return true;
        }

        private void InitiOS()
        {
            ImageLoaderHelpers.BitmapFunction = BitmapReader;
            ImageLoaderHelpers.SavePNGFunc = SavePNG;

            FSOProgram.ShowDialog = ShowDialog;
            ITTSContext.Provider = AppleTTSContext.PlatformProvider;
            iOSKeyboard.Initialize();
        }

        private void RunGame()
        {
            // Hide installer window before MonoGame creates its own
            if (_window != null)
            {
                _window.Hidden = true;
                _window = null;
            }

            InitiOS();

            // Calculate DPI scale factor (same approach as Android/FSODroid)
            var nativeScale = (int)UIScreen.MainScreen.NativeScale;
            var nativeBounds = UIScreen.MainScreen.NativeBounds;
            var width = (int)Math.Max(nativeBounds.Width, nativeBounds.Height);
            var height = (int)Math.Min(nativeBounds.Width, nativeBounds.Height);

            var initSF = nativeScale;
            var dpiScaleFactor = initSF;
            float uiZoomFactor = 1f;
            while (width / dpiScaleFactor < 800 && dpiScaleFactor > 1)
            {
                dpiScaleFactor--;
                uiZoomFactor = (float)initSF / dpiScaleFactor;
            }

            FSOEnvironment.ContentDir = "Content/";
            FSOEnvironment.GFXContentDir = "Content/iOS/";
            if (!Directory.Exists(FSOEnvironment.GFXContentDir))
                FSOEnvironment.GFXContentDir = "Content/OGL/";

            FSOEnvironment.UserDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            FSOEnvironment.Linux = true;
            FSOEnvironment.DirectX = false;
            FSOEnvironment.SoftwareKeyboard = true;
            FSOEnvironment.SoftwareDepth = true;
            FSOEnvironment.EnableNPOTMip = true;
            FSOEnvironment.GLVer = 2;
            FSOEnvironment.UseMRT = false;
            FSOEnvironment.UIZoomFactor = uiZoomFactor;
            FSOEnvironment.DPIScaleFactor = dpiScaleFactor;
            FSOEnvironment.TexCompress = false;
            FSOEnvironment.TexCompressSupport = false;
            FSOEnvironment.GameThread = Thread.CurrentThread;
            FSOEnvironment.Enable3D = true;

            ImageLoader.UseSoftLoad = false;

            GlobalSettings.Default.CityShadows = false;
            var set = GlobalSettings.Default;
            set.TargetRefreshRate = 60;
            set.CurrentLang = "english";
            set.Lighting = true;
            set.SmoothZoom = true;
            set.AntiAlias = 0;
            set.LightingMode = 3;
            set.AmbienceVolume = 10;
            set.FXVolume = 10;
            set.MusicVolume = 10;
            set.VoxVolume = 10;
            set.DPIScaleFactor = dpiScaleFactor;
            set.GraphicsWidth = width / dpiScaleFactor;
            set.GraphicsHeight = height / dpiScaleFactor;
            set.Windowed = false;
            set.DirectionalLight3D = false;

            // Clean up leftover zip if present
            var zipPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "The Sims Online.zip");
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            var start = new GameStartProxy();
            start.SetPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "The Sims Online/TSOClient/"));

            Console.WriteLine($"[FSO] NativeBounds: {nativeBounds.Width}x{nativeBounds.Height}, NativeScale: {nativeScale}");
            Console.WriteLine($"[FSO] Landscape: {width}x{height}, DPIScale: {dpiScaleFactor}, UIZoom: {uiZoomFactor}");
            Console.WriteLine($"[FSO] GraphicsWidth: {set.GraphicsWidth}, GraphicsHeight: {set.GraphicsHeight}");
            Console.WriteLine($"[FSO] Bounds: {UIScreen.MainScreen.Bounds.Width}x{UIScreen.MainScreen.Bounds.Height}");

            TSOGame game = new TSOGame();
            GameFacade.DirectX = false;
            FSO.LotView.World.DirectX = false;
            game.Run(GameRunBehavior.Asynchronous);
        }

        private static void ShowDialog(string text)
        {
            InvokeOnMainThread(() =>
            {
                var alert = UIAlertController.Create("FreeSO", text, UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));

                var rootVC = UIApplication.SharedApplication.KeyWindow?.RootViewController;
                rootVC?.PresentViewController(alert, true, null);
            });
        }

        private static void InvokeOnMainThread(Action action)
        {
            if (NSThread.IsMain)
                action();
            else
                NSRunLoop.Main.BeginInvokeOnMainThread(action);
        }

        public static void SavePNG(byte[] data, int width, int height, Stream str)
        {
            using var image = new Image<Rgba32>(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    image[x, y] = new Rgba32(data[i], data[i + 1], data[i + 2], data[i + 3]);
                }
            }

            image.Save(str, new PngEncoder());
        }

        public static Tuple<byte[], int, int> BitmapReader(Stream str)
        {
            using var image = Image.Load<Rgba32>(str);
            int width = image.Width;
            int height = image.Height;

            var data = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    Rgba32 px = image[x, y];
                    data[i] = px.R;
                    data[i + 1] = px.G;
                    data[i + 2] = px.B;
                    data[i + 3] = px.A;
                }
            }

            return new Tuple<byte[], int, int>(data, width, height);
        }
    }
}
