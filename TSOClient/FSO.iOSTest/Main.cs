using Foundation;
using FSO.Client;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Files;
using FSO.LotView;
using Microsoft.Xna.Framework.Input;
using UIKit;

namespace FSO.iOS
{
    [Register("AppDelegate")]
    internal class Program : UIApplicationDelegate
    {
        public static Action<string>? MainOrg;

		private void RunGame()
		{
            InvokeOnMainThread(() => { ShowDialog("Trying to start FreeSO MonoGame..."); });

            // ImageLoader.BaseFunction = iOSImageLoader.iOSFromStream;

            /*
            var settings = new NinjectSettings();
            settings.LoadExtensions = false;
            */

            InvokeOnMainThread(() =>
            {
                if (MainOrg != null)
                {
                    ShowDialog("Falling into MainOrg...");
                    // var cont = new FSO.Client.GameController(null);
                }

                MainOrg = FSO.Client.FSOProgram.ShowDialog;
            });

            // GlobalSettings.Default.CityShadows = false;

/*
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
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip")))
            {
                InvokeOnMainThread(() => { 
                    ShowDialog("Cleaning up temporary downloaded TSO file...");
                });
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip"));
            }
            */
   
            // TSOGame game = new TSOGame();
            // GameFacade.DirectX = false;
            // FSO.LotView.World.DirectX = false;
            InvokeOnMainThread(() => { 
                // FSOEnvironment.GameThread = Thread.CurrentThread;
                // game.Run();
                
                if ((new FSOProgram()).InitWithArguments(new string[] { }))
                {
                    ShowDialog("Going to run FreeSO MonoGame now...");
                    var startProxy = new GameStartProxy();
                    startProxy.SetPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/"));//"/private/var/mobile/Documents/The Sims Online/TSOClient/");
                    TSOGame game = new TSOGame();
                    GameFacade.DirectX = false;
                    FSO.LotView.World.DirectX = false;
                    game.Run();
                }
                else
                {
                    ShowDialog("FSOProgram InitWithArguments failed!");
                }
            });
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // FSOProgram.ShowDialog = ShowDialog;
            
            UIApplication.Main(args, null, "AppDelegate");
        }
        
        private UIWindow? window;
        private FSOInstallViewController? installerVC;

        public override void FinishedLaunching(UIApplication app)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var tsoClientPath = Path.Combine(docs, "The Sims Online/TSOClient/tuning.dat");
            
            // print app files in documents for debugging in showDialog
            var appFiles = Directory.GetFiles(docs, "*", SearchOption.AllDirectories);
            var fileList = "App files:\n" + string.Join("\n", appFiles);
            InvokeOnMainThread(() => { ShowDialog(fileList); });

            if (File.Exists(tsoClientPath))
            {
                // RunGame();
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
            InvokeOnMainThread(() =>
            {
                ShowDialog("FreeSO installed, starting game...");
                RunGame();
            });
        }
        
        public static void ShowDialog(string text)
        {
            UIAlertView _alert = new UIAlertView("FreeSO Message", text, null, "OK", null);
            _alert.Show();
            Task.Delay(2000).Wait();
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