using Foundation;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using UIKit;
using CoreGraphics;
using FSO.Client;

namespace FSO.iOS
{
    public class FSOInstallViewController : UIViewController
    {
        UITextField IPEntry;
        UIButton IpConfirm;
        UILabel StatusText;
        UIProgressView StatusProgress;
        UIImageView SplashImage;

        private bool ReDownload = false;
        public event Action OnInstalled;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = UIColor.White;

            // --- Splash Image ---
            SplashImage = new UIImageView(UIImage.FromBundle("FreeSO.png"))
            {
                ContentMode = UIViewContentMode.ScaleAspectFit,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(SplashImage);

            // --- Status Label ---
            StatusText = new UILabel
            {
                Text = "Enter a location to download The Sims Online files from.",
                TextAlignment = UITextAlignment.Center,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(StatusText);

            // --- Progress View ---
            StatusProgress = new UIProgressView(UIProgressViewStyle.Default)
            {
                Progress = 0f,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(StatusProgress);

            // --- IP Entry ---
            IPEntry = new UITextField
            {
                Placeholder = "Enter PC IP",
                Text = "127.0.0.1:8080",
                BorderStyle = UITextBorderStyle.RoundedRect,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            IPEntry.ShouldReturn += (textField) =>
            {
                textField.ResignFirstResponder();
                return true;
            };
            View.AddSubview(IPEntry);

            // --- Go Button ---
            IpConfirm = new UIButton(UIButtonType.RoundedRect)
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            IpConfirm.SetTitle("Go", UIControlState.Normal);
            IpConfirm.TouchUpInside += (sender, e) => StartDownload();
            View.AddSubview(IpConfirm);

            // --- Tap to dismiss keyboard ---
            var tap = new UITapGestureRecognizer(() => View.EndEditing(true));
            View.AddGestureRecognizer(tap);

            ShowAlert("Welcome!",
                "To run FreeSO on iOS, you must transfer The Sims Online game files into this app. For instructions, see the forums.");

            // --- Auto Layout Constraints ---
            NSLayoutConstraint.ActivateConstraints(new[]
            {
                SplashImage.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                SplashImage.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 20),
                SplashImage.WidthAnchor.ConstraintEqualTo(512),
                SplashImage.HeightAnchor.ConstraintEqualTo(128),

                StatusText.TopAnchor.ConstraintEqualTo(SplashImage.BottomAnchor, 20),
                StatusText.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 20),
                StatusText.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -20),
                StatusText.HeightAnchor.ConstraintEqualTo(30),

                StatusProgress.TopAnchor.ConstraintEqualTo(StatusText.BottomAnchor, 10),
                StatusProgress.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 20),
                StatusProgress.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -20),
                StatusProgress.HeightAnchor.ConstraintEqualTo(10),

                IPEntry.TopAnchor.ConstraintEqualTo(StatusProgress.BottomAnchor, 20),
                IPEntry.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 20),
                IPEntry.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -120),
                IPEntry.HeightAnchor.ConstraintEqualTo(30),

                IpConfirm.CenterYAnchor.ConstraintEqualTo(IPEntry.CenterYAnchor),
                IpConfirm.LeadingAnchor.ConstraintEqualTo(IPEntry.TrailingAnchor, 8),
                IpConfirm.WidthAnchor.ConstraintEqualTo(80),
                IpConfirm.HeightAnchor.ConstraintEqualTo(30)
            });
        }

        private void ResetDownloader()
        {
            StatusText.Text = "Enter a location to download TSO files from.";
            StatusProgress.Progress = 0f;
            IpConfirm.Enabled = true;
            IPEntry.Enabled = true;

            try
            {
                var zipPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TheSimsOnline.zip");
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                var extractPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TheSimsOnline");
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
            }
            catch (Exception e)
            {
                ShowAlert("Error", "Failed to clean up: " + e.Message);
            }

            ReDownload = true;
        }

        private async void StartDownload()
        {
            IpConfirm.Enabled = false;
            IPEntry.Enabled = false;

            var url = "http://" + IPEntry.Text + "/The%20Sims%20Online.zip";
            var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TheSimsOnline.zip");
            var extractPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TheSimsOnline");

            try
            {
                // Download ZIP if needed
                if (ReDownload || !File.Exists(dest))
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            InvokeOnMainThread(() =>
                            {
                                StatusText.Text = $"Downloading TSO Files... ({e.ProgressPercentage}%)";
                                StatusProgress.Progress = e.ProgressPercentage / 100f;
                            });
                        };

                        await client.DownloadFileTaskAsync(new Uri(url), dest);
                    }
                }

                // Verify ZIP file
                var fileInfo = new FileInfo(dest);
                if (!fileInfo.Exists || fileInfo.Length == 0)
                    throw new Exception("Downloaded zip file is missing or empty.");

                // Prepare extraction directory
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                Directory.CreateDirectory(extractPath);

                StatusText.Text = "Extracting TSO Files...";
                StatusProgress.Progress = 0f;

                // Safe extraction
                await Task.Run(() => ExtractZipStreamed(dest, extractPath));

                // Done
                InvokeOnMainThread(() =>
                {
                    StatusText.Text = "Extraction Complete!";
                    StatusProgress.Progress = 1f;

                    var alert = UIAlertController.Create("Ready", 
                        "The Sims Online files have been extracted. Do you want to launch FreeSO now?", 
                        UIAlertControllerStyle.Alert);

                    alert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null));
                    alert.AddAction(UIAlertAction.Create("Launch Game", UIAlertActionStyle.Default, (action) =>
                    {
                        OnInstalled?.Invoke();
                    }));

                    PresentViewController(alert, true, null);
                });
            }
            catch (Exception ex)
            {
                InvokeOnMainThread(() =>
                {
                    ShowAlert("Error", "An error occurred: " + ex.ToString());
                    ResetDownloader();
                });
            }
        }

        private void ExtractZipStreamed(string zipPath, string extractPath)
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                int totalEntries = archive.Entries.Count;
                int current = 0;

                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue; // skip directories

                    var entryPath = Path.Combine(extractPath, entry.FullName);
                    var directory = Path.GetDirectoryName(entryPath);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    try
                    {
                        // More memory-efficient extraction
                        entry.ExtractToFile(entryPath, overwrite: true);
                    }
                    catch
                    {
                        // Fallback: manual streaming with small buffer
                        using (var entryStream = entry.Open())
                        using (var fileStream = new FileStream(entryPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: false))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }

                    current++;
                    if (current % 10 == 0)
                    {
                        int progress = (int)((current / (float)totalEntries) * 100);
                        InvokeOnMainThread(() =>
                        {
                            StatusProgress.Progress = progress / 100f;
                            StatusText.Text = $"Extracting TSO Files... ({progress}%)";
                        });
                    }
                }
            }
        }

        private void ShowAlert(string title, string message)
        {
            InvokeOnMainThread(() =>
            {
                FSOProgram.ShowDialog(title + " " + message);
            });
        }
    }
}
