using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;



namespace FilipPG
{
    public static class UpdateChecker
    {
        private const string updateUrl = "https://sapientechapps.github.io/FilipPG/update-info.json";

        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync(updateUrl);

                var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(response);

                if (updateInfo != null &&
                    Version.TryParse(updateInfo.latestVersion, out var latest) &&
                    Version.TryParse(VerziaPG.Aktualna, out var current) &&
                    latest > current)
                {
                    ShowUpdateNotification(updateInfo);
                }
            }
            catch (Exception ex)
            {
                // Nepanik�rime, update check m��e pokojne zlyha� bez hlu�n�ho v�pisu
                Console.WriteLine($"Chyba pri kontrole aktualiz�cie: {ex.Message}");
            }
        }

        private static void ShowUpdateNotification(UpdateInfo info)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), "FilipPG_Update.exe");

            using var webClient = new WebClient();
            try
            {
                webClient.DownloadFile(info.updateUrl, tempFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nepodarilo sa stiahnu� aktualiz�ciu:\n{ex.Message}", "Chyba aktualiz�cie", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                BalloonTipTitle = $"Nov� verzia {info.latestVersion} je pripraven�!",
                BalloonTipText = "Kliknite pre spustenie novej verzie.",
            };

            trayIcon.BalloonTipClicked += (s, e) =>
            {
                try
                {
                    Process.Start(tempFilePath);
                    Environment.Exit(0); // ukon�� aktu�lnu verziu
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Spustenie novej verzie zlyhalo:\n{ex.Message}", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            trayIcon.ShowBalloonTip(5000);
        }


        private class UpdateInfo
        {
            public string latestVersion { get; set; } = "";
            public string build { get; set; } = "";
            public string releaseNotes { get; set; } = "";
            public string updateUrl { get; set; } = "";
        }
    }
}
