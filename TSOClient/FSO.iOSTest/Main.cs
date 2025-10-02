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
        // public static Action<string>? MainOrg;

		internal static void RunGame()
		{
            
            // ImageLoader.BaseFunction = iOSImageLoader.iOSFromStream;

            /*
            var settings = new NinjectSettings();
            settings.LoadExtensions = false;
            */

            // InvokeOnMainThread(() =>
            // {
            //     if (MainOrg != null)
            //     {
            //         ShowDialog("Falling into MainOrg...");
            //         // var cont = new FSO.Client.GameController(null);
            //     }
            //
            // MainOrg = FSO.Client.FSOProgram.ShowDialog;
            // });
            
                FSOEnvironment.GameThread = Thread.CurrentThread;
                // game.Run();

                
                // ShowDialog("Trying to start FreeSO MonoGame...");

                var startProxy = new GameStartProxy();
                startProxy.SetPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/")); //"/private/var/mobile/Documents/The Sims Online/TSOClient/");
                // startProxy.Start(false);
                // new FSOProgram().InitWithArguments(new string[] { });
                ShowDialog("Going to run FreeSO MonoGame now...");
                
                var game = new TSOGame();
                GameFacade.DirectX = false;
                FSO.LotView.World.DirectX = false;
                game.Run();
            
            /*else
            {
                ShowDialog("FSOProgram InitWithArguments failed!");
            }*/
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
            if (window != null)
            {
                window.RootViewController = null;
                window.Dispose();
                window = null;
            }
            if (installerVC != null)
            {
                installerVC.OnInstalled -= FSOInstalled;
                installerVC.Dispose();
                installerVC = null;
            }
            
            RunGame();
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