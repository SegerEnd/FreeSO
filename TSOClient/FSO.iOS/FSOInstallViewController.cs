using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;

namespace FSO.iOS
{
    public class FSOInstallViewController : UIViewController
    {
        private UITextField _ipEntry;
        private UIButton _confirmButton;
        private UILabel _statusText;
        private UIProgressView _progressView;
        private HttpClient _httpClient;
        private bool _reDownload;

        public event Action OnInstalled;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = UIColor.Black;

            var titleLabel = new UILabel
            {
                Text = "FreeSO",
                TextColor = UIColor.White,
                Font = UIFont.BoldSystemFontOfSize(28),
                TextAlignment = UITextAlignment.Center,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var instructionLabel = new UILabel
            {
                Text = "Enter the IP address of a PC hosting TSO files:",
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(14),
                TextAlignment = UITextAlignment.Center,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _ipEntry = new UITextField
            {
                Placeholder = "192.168.1.1",
                BorderStyle = UITextBorderStyle.RoundedRect,
                KeyboardType = UIKeyboardType.Url,
                AutocapitalizationType = UITextAutocapitalizationType.None,
                AutocorrectionType = UITextAutocorrectionType.No,
                TextAlignment = UITextAlignment.Center,
                BackgroundColor = UIColor.White,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _ipEntry.ShouldReturn += (textField) =>
            {
                textField.ResignFirstResponder();
                return true;
            };

            _confirmButton = new UIButton(UIButtonType.System);
            _confirmButton.SetTitle("Download", UIControlState.Normal);
            _confirmButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            _confirmButton.BackgroundColor = UIColor.SystemBlue;
            _confirmButton.Layer.CornerRadius = 8;
            _confirmButton.TranslatesAutoresizingMaskIntoConstraints = false;
            _confirmButton.TouchUpInside += OnConfirmTapped;

            _statusText = new UILabel
            {
                Text = "Enter a location to download TSO files from.",
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(12),
                TextAlignment = UITextAlignment.Center,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _progressView = new UIProgressView(UIProgressViewStyle.Default)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Progress = 0f
            };

            View.AddSubview(titleLabel);
            View.AddSubview(instructionLabel);
            View.AddSubview(_ipEntry);
            View.AddSubview(_confirmButton);
            View.AddSubview(_statusText);
            View.AddSubview(_progressView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                titleLabel.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                titleLabel.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 40),

                instructionLabel.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                instructionLabel.TopAnchor.ConstraintEqualTo(titleLabel.BottomAnchor, 20),

                _ipEntry.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                _ipEntry.TopAnchor.ConstraintEqualTo(instructionLabel.BottomAnchor, 12),
                _ipEntry.WidthAnchor.ConstraintEqualTo(250),
                _ipEntry.HeightAnchor.ConstraintEqualTo(36),

                _confirmButton.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                _confirmButton.TopAnchor.ConstraintEqualTo(_ipEntry.BottomAnchor, 12),
                _confirmButton.WidthAnchor.ConstraintEqualTo(150),
                _confirmButton.HeightAnchor.ConstraintEqualTo(40),

                _statusText.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                _statusText.TopAnchor.ConstraintEqualTo(_confirmButton.BottomAnchor, 20),

                _progressView.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                _progressView.TopAnchor.ConstraintEqualTo(_statusText.BottomAnchor, 8),
                _progressView.WidthAnchor.ConstraintEqualTo(250),
            });

            var tapGesture = new UITapGestureRecognizer(() => View.EndEditing(true));
            View.AddGestureRecognizer(tapGesture);

            ShowAlert("Welcome!",
                "To run FreeSO on iOS, you must transfer the TSO game files into this app. " +
                "For instructions, see the forums.");
        }

        private async void OnConfirmTapped(object sender, EventArgs e)
        {
            _confirmButton.Enabled = false;
            _ipEntry.Enabled = false;

            var host = string.IsNullOrWhiteSpace(_ipEntry.Text) ? "192.168.1.1" : _ipEntry.Text.Trim();
            var url = host.Contains("://")
                ? $"{host}/The%20Sims%20Online.zip"
                : $"https://{host}/The%20Sims%20Online.zip";
            var httpFallback = host.Contains("://")
                ? null
                : $"http://{host}/The%20Sims%20Online.zip";
            var dest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "The Sims Online.zip");

            bool needsDownload = _reDownload || !File.Exists(dest);

            // Validate existing zip before skipping download
            if (!needsDownload)
            {
                try
                {
                    using (var test = ZipFile.OpenRead(dest)) { }
                }
                catch
                {
                    // Corrupt/incomplete zip from previous attempt - re-download
                    try { File.Delete(dest); } catch { }
                    needsDownload = true;
                }
            }

            if (needsDownload)
            {
                await DownloadFileAsync(url, httpFallback, dest);
            }
            else
            {
                await ExtractAndFinish(dest);
            }
        }

        private async System.Threading.Tasks.Task DownloadFileAsync(string url, string httpFallback, string dest)
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = new HttpClient();

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                }
                catch when (httpFallback != null)
                {
                    InvokeOnMainThread(() => _statusText.Text = "HTTPS failed, trying HTTP...");
                    response = await _httpClient.GetAsync(httpFallback, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                }
                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                long bytesRead = 0;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int read;
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        bytesRead += read;

                        if (totalBytes > 0)
                        {
                            var progress = (float)bytesRead / totalBytes;
                            InvokeOnMainThread(() =>
                            {
                                _statusText.Text = $"Downloading TSO Files... ({(int)(progress * 100)}%)";
                                _progressView.Progress = progress;
                            });
                        }
                    }
                }
                response.Dispose();

                await ExtractAndFinish(dest);
            }
            catch (Exception ex)
            {
                InvokeOnMainThread(() =>
                {
                    ShowAlert("An error occurred",
                        "An error occurred during your download. Please try again, " +
                        "and make sure the connection to your PC is stable!\n\n" + ex.Message);
                    ResetDownloader();
                });
            }
        }

        private async System.Threading.Tasks.Task ExtractAndFinish(string zipPath)
        {
            InvokeOnMainThread(() =>
            {
                _statusText.Text = "Extracting TSO Files...";
                _progressView.Progress = 0f;
            });

            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var extractPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "The Sims Online/");
                    Directory.CreateDirectory(extractPath);

                    using (var archive = ZipFile.OpenRead(zipPath))
                    {
                        // Delete the zip file while it's still open to free disk space
                        // (Unix semantics: file stays readable via the open handle)
                        try { File.Delete(zipPath); } catch { }

                        var total = archive.Entries.Count;
                        var count = 0;
                        var stripPrefix = "The Sims Online/";
                        foreach (var entry in archive.Entries)
                        {
                            count++;
                            // Strip the top-level "The Sims Online/" prefix since
                            // we're already extracting into that directory
                            var entryPath = entry.FullName;
                            if (entryPath.StartsWith(stripPrefix))
                                entryPath = entryPath.Substring(stripPrefix.Length);
                            if (string.IsNullOrEmpty(entryPath))
                                continue;
                            var destPath = Path.Combine(extractPath, entryPath);

                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                Directory.CreateDirectory(destPath);
                                continue;
                            }

                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            entry.ExtractToFile(destPath, true);

                            if (count % 50 == 0)
                            {
                                var pct = (int)((float)count / total * 100);
                                InvokeOnMainThread(() =>
                                {
                                    _statusText.Text = $"Extracting TSO Files... ({pct}%) [{count}/{total}]";
                                    _progressView.Progress = (float)count / total;
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    InvokeOnMainThread(() =>
                    {
                        ShowAlert("An error occurred",
                            "Fatal error occurred during zip extraction. " + ex.ToString());
                        ResetDownloader();
                    });
                    return;
                }

                // Clean up zip file if it still exists
                try { File.Delete(zipPath); } catch { }

                InvokeOnMainThread(() =>
                {
                    try
                    {
                        OnInstalled?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        ShowAlert("An error occurred",
                            "Game failed to start: " + ex.ToString());
                    }
                });
            });
        }

        private void ResetDownloader()
        {
            _statusText.Text = "Enter a location to download TSO files from.";
            _progressView.Progress = 0f;
            _confirmButton.Enabled = true;
            _ipEntry.Enabled = true;
            _reDownload = true;

            try
            {
                var zipPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "The Sims Online.zip");
                File.Delete(zipPath);
            }
            catch { }

            _httpClient?.Dispose();
            _httpClient = null;
        }

        private void ShowAlert(string title, string message)
        {
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
            PresentViewController(alert, true, null);
        }
    }
}
