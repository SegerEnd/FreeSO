using Foundation;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
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

        private WebClient DownloadClient;
        public event Action OnInstalled;
        private bool ReDownload = false;

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

            // --- Tap anywhere to dismiss keyboard ---
            var tap = new UITapGestureRecognizer(() => View.EndEditing(true));
            View.AddGestureRecognizer(tap);
            
            ShowAlert("Welcome!", "To run FreeSO on iOS, you must transfer the The Sims Online game files into this app. For instructions, see the forums.");

            // --- Auto Layout Constraints ---
            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // Splash Image
                SplashImage.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                SplashImage.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 20),
                SplashImage.WidthAnchor.ConstraintEqualTo(512),
                SplashImage.HeightAnchor.ConstraintEqualTo(128),

                // Status Label
                StatusText.TopAnchor.ConstraintEqualTo(SplashImage.BottomAnchor, 20),
                StatusText.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 20),
                StatusText.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -20),
                StatusText.HeightAnchor.ConstraintEqualTo(30),

                // Progress
                StatusProgress.TopAnchor.ConstraintEqualTo(StatusText.BottomAnchor, 10),
                StatusProgress.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 20),
                StatusProgress.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -20),
                StatusProgress.HeightAnchor.ConstraintEqualTo(10),

                // IP Entry
                IPEntry.TopAnchor.ConstraintEqualTo(StatusProgress.BottomAnchor, 20),
                IPEntry.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 20),
                IPEntry.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -120),
                IPEntry.HeightAnchor.ConstraintEqualTo(30),

                // Go Button
                IpConfirm.CenterYAnchor.ConstraintEqualTo(IPEntry.CenterYAnchor),
                IpConfirm.LeadingAnchor.ConstraintEqualTo(IPEntry.TrailingAnchor, 8),
                IpConfirm.WidthAnchor.ConstraintEqualTo(80),
                IpConfirm.HeightAnchor.ConstraintEqualTo(30)
            });
        }

        private async void StartDownload()
        {
            IpConfirm.Enabled = false;
            IPEntry.Enabled = false;

            string url = "http://" + (IPEntry?.Text ?? "127.0.0.1") + "/The Sims Online.zip";
            string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip");

            try
            {
                using (var client = new System.Net.Http.HttpClient())
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var receivedBytes = 0L;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(dest))
                    {
                        var buffer = new byte[81920];
                        int bytesRead;
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            receivedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                float progress = (float)receivedBytes / totalBytes;
                                InvokeOnMainThread(() =>
                                {
                                    StatusProgress.Progress = progress;
                                    StatusText.Text = $"Downloading The Sims Online Files... ({(int)(progress * 100)}%)";
                                });
                            }
                        }
                    }
                }

                // Extraction
                new Thread(() =>
                {
                    string extractPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online");
                    ExtractZipStreamedAsync(dest, extractPath).Wait();
                }).Start();
            }
            catch (Exception ex)
            {
                ShowAlert("Download Error", "Failed to download: " + ex.Message);
                ResetDownloader();
            }
        }

        private async Task ExtractZipStreamedAsync(string zipPath, string extractPath)
        {
            try
            {
                // Ensure the target directory exists
                Directory.CreateDirectory(extractPath);

                using (var zipFile = File.OpenRead(zipPath))
                using (var archive = new ZipArchive(zipFile, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var destination = Path.Combine(extractPath, entry.FullName);

                        if (string.IsNullOrEmpty(entry.Name)) // Folder
                        {
                            Directory.CreateDirectory(destination);
                            continue;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                        // Use async copy to avoid blocking
                        using (var entryStream = entry.Open())
                        using (var fileStream = File.Create(destination))
                        {
                            await entryStream.CopyToAsync(fileStream);
                        }
                    }
                }

                // Safely invoke OnInstalled on the main thread asynchronously
                BeginInvokeOnMainThread(() =>
                {
                    OnInstalled?.Invoke();
                });
            }
            catch (Exception ex)
            {
                BeginInvokeOnMainThread(() =>
                {
                    ShowAlert("Extraction Error", $"Failed during zip extraction: {ex.Message}");
                    ResetDownloader();
                });
            }
        }

        private void ResetDownloader()
        {
            StatusText.Text = "Enter a location to download The Sims Online files from.";
            StatusProgress.Progress = 0f;
            IpConfirm.Enabled = true;
            IPEntry.Enabled = true;

            try
            {
                string zipPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);
            }
            catch { }

            ReDownload = true;

            if (DownloadClient != null)
            {
                DownloadClient.Dispose();
                DownloadClient = null;
            }
        }

        private void ShowAlert(string title, string message)
        {
            FSOProgram.ShowDialog(title + " " + message);
        }
    }
}
