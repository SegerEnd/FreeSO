using Foundation;
using FSO.Client;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Files;
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
            FSOEnvironment.GLVer = 2;
            FSOEnvironment.UseMRT = false;
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
            set.CurrentLang = "english";
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
            set.CitySelectorUrl = "http://46.101.67.219:8081";
            set.GameEntryUrl = "http://46.101.67.219:8081";

            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip")))
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip"));

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
            UIApplication.Main(args, null, "AppDelegate");
        }

        public override void FinishedLaunching(UIApplication app)
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/tuning.dat")))
            {
                RunGame();
            }
            else
            {
                UIStoryboard storyboard = UIStoryboard.FromName("Installer", null);
                var window = new UIWindow(UIScreen.MainScreen.Bounds);
                var viewController = storyboard.InstantiateViewController("Main") as FSOInstallViewController;
                viewController.OnInstalled += FSOInstalled;
                window.MakeKeyAndVisible();
                window.RootViewController = viewController;
            }
        }

        private void FSOInstalled()
        {
            RunGame();
        }
        
        /*
        public static void ShowDialog(string text)
        {
            var message = text;

            // Find the active foreground scene
            foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
            {
                if (scene is UIWindowScene windowScene && windowScene.ActivationState == UISceneActivationState.ForegroundActive)
                {
                    var window = windowScene.Windows.FirstOrDefault(w => w.IsKeyWindow);
                    var rootVC = window?.RootViewController;
                    if (rootVC == null) continue;

                    var topVC = GetTopViewController(rootVC);

                    // Must present on the main thread
                    UIApplication.SharedApplication.InvokeOnMainThread(() =>
                    {
                        var alert = UIAlertController.Create("FSO Message", message, UIAlertControllerStyle.Alert);
                        alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
                        topVC.PresentViewController(alert, true, null);
                    });

                    break; // only show on one active scene
                }
            }
        }

        private static UIViewController GetTopViewController(UIViewController root)
        {
            if (root.PresentedViewController == null)
                return root;

            if (root.PresentedViewController is UINavigationController nav)
                return GetTopViewController(nav.VisibleViewController);

            if (root.PresentedViewController is UITabBarController tab)
                return GetTopViewController(tab.SelectedViewController);

            return GetTopViewController(root.PresentedViewController);
        }
        */
    }
}