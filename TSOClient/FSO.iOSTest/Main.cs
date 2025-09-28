using Foundation;
using FSO.Client;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Files;
using Microsoft.Xna.Framework.Input;
using UIKit;

namespace FSO.iOS
{
    [Register("AppDelegate")]
    internal class Program : UIApplicationDelegate
    {
        public static Action<string> MainOrg;

		internal static void RunGame()
		{
            ImageLoader.BaseFunction = iOSImageLoader.iOSFromStream;
            var iPad = UIDevice.CurrentDevice.Model.Contains("iPad");
            //TODO: disable iPad retina somehow
            FSOEnvironment.ContentDir = "Content/";
			FSOEnvironment.GFXContentDir = "Content/iOS/";
			FSOEnvironment.UserDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			FSOEnvironment.Linux = true;
			FSOEnvironment.DirectX = false;
			FSOEnvironment.SoftwareKeyboard = true;
			FSOEnvironment.SoftwareDepth = true;
            FSOEnvironment.EnableNPOTMip = true;
            // FSOEnvironment.GLVer = 2;
            // FSOEnvironment.UseMRT = false;
			FSOEnvironment.UIZoomFactor = iPad?1:2;
            FSOEnvironment.DPIScaleFactor = iPad ? 2 : 1;
            FSOEnvironment.TexCompress = false;
            FSOEnvironment.TexCompressSupport = false;

            FSOEnvironment.GameThread = Thread.CurrentThread;
            FSOEnvironment.Enable3D = true;
            ITTSContext.Provider = AppleTTSContext.PlatformProvider;

            FSO.Files.ImageLoader.UseSoftLoad = false;

            /*
            var settings = new NinjectSettings();
            settings.LoadExtensions = false;
            */

            if (MainOrg != null)
            {
                var cont = new FSO.Client.GameController(null);
            }
            MainOrg = FSO.Client.FSOProgram.ShowDialog;

            GlobalSettings.Default.CityShadows = false;


            var set = GlobalSettings.Default;
            set.TargetRefreshRate = 60;
            // set.CurrentLang = "english";
            set.Lighting = true;
            set.SmoothZoom = true;
            set.AntiAlias = 0;
            set.LightingMode = 3;
            set.AmbienceVolume = 10;
            set.FXVolume = 10;
            set.MusicVolume = 10;
            set.VoxVolume = 10;
            set.GraphicsWidth = (int)UIScreen.MainScreen.Bounds.Width;
            set.DirectionalLight3D = false;
            set.GraphicsHeight = (int)UIScreen.MainScreen.Bounds.Height;
            // set.CitySelectorUrl = "http://46.101.67.219:8081";
            // set.GameEntryUrl = "http://46.101.67.219:8081";

            // if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip")))
            //     File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip"));

			var start = new GameStartProxy();
            start.SetPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/"));//"/private/var/mobile/Documents/The Sims Online/TSOClient/");

            TSOGame game = new TSOGame();
            GameFacade.DirectX = false;
            FSO.LotView.World.DirectX = false;
            game.Run(Microsoft.Xna.Framework.GameRunBehavior.Asynchronous);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            FSOProgram.ShowDialog = ShowDialog;
            
            UIApplication.Main(args, null, "AppDelegate");
        }
        
        private UIWindow? window;
        private FSOInstallViewController? installerVC;

        public override void FinishedLaunching(UIApplication app)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var tsoClientPath = Path.Combine(docs, "The Sims Online/TSOClient/tuning.dat");

            if (File.Exists(tsoClientPath))
            {
                RunGame();
            }
            else
            {
                window = new UIWindow(UIScreen.MainScreen.Bounds);
                installerVC = new FSOInstallViewController();
                installerVC.OnInstalled += FSOInstalled;

                window.RootViewController = installerVC;
                window.MakeKeyAndVisible();
            }
        }

        private void FSOInstalled()
        {
            RunGame();
        }
        
        public static void ShowDialog(string text)
        {
            // Escape double quotes in text
            string escapedText = text.Replace("\"", "\\\"");

            var alertController = UIAlertController.Create("FreeSO Message", escapedText, UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));

            // Get the topmost view controller
            var window = UIApplication.SharedApplication.KeyWindow;
            var viewController = window.RootViewController;
            while (viewController.PresentedViewController != null)
            {
                viewController = viewController.PresentedViewController;
            }

            viewController.PresentViewController(alertController, true, null);
            
            Environment.Exit(1);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject;
            
            if (exception is OutOfMemoryException)
            {
                ShowDialog(e.ExceptionObject.ToString() + "Out of Memory! FreeSO needs to close.");
            }
            else
            {
                ShowDialog(e.ExceptionObject.ToString() + "A fatal error occured! Screenshot this dialog and post it on Discord.");
            }
            
            Environment.Exit(1);
        }
    }
}