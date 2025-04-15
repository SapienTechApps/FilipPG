// FilipPG - verzia s tray ikonou, menu a tlačidlom „Stíš Filipa“ + logika pre záťaž

using System;
using System.Diagnostics;
using System.Management;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Timer = System.Timers.Timer;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace FilipPG
{
    public partial class MainWindow : Window
    {
        private readonly PerformanceCounter cpuCounter;
        private readonly PerformanceCounter ramCounter;
        private readonly Timer updateTimer;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private DateTime lastCpuAlertTime = DateTime.MinValue;
        private DateTime lastRamAlertTime = DateTime.MinValue;
        private const int alertCooldownMinutes = 10;

        public MainWindow()
        {
            InitializeComponent();
            InitTray();

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            ModeText.Text = "Režim: ECO";

            UpdateNow();

            updateTimer = new Timer(60000);
            updateTimer.Elapsed += (s, e) => UpdateNow();
            updateTimer.Start();
            _ = UpdateChecker.CheckForUpdatesAsync();

        }

        private void InitTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Otvoriť", null, (s, e) => this.Dispatcher.Invoke(() => this.Show()));
            trayMenu.Items.Add("Stíš Filipa", null, (s, e) => this.Dispatcher.Invoke(() => this.Hide()));
            trayMenu.Items.Add("Ukončiť", null, (s, e) => CloseApp());

            trayIcon = new NotifyIcon
            {
                Text = "FilipPG - beží v pozadí",
                Icon = SystemIcons.Information,
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick += (s, e) => this.Dispatcher.Invoke(() => this.Show());
        }

        private void CloseApp()
        {
            trayIcon.Visible = false;
            updateTimer?.Stop();
            System.Windows.Application.Current.Shutdown();
        }

        private void UpdateNow()
        {
            float cpu = cpuCounter.NextValue();
            float ram = ramCounter.NextValue();

            Task.Delay(1000).ContinueWith(_ =>
            {
                float realCpu = cpuCounter.NextValue();
                float freeRam = ramCounter.NextValue();
                float totalRam = GetTotalPhysicalMemoryInMB();
                float usedRam = totalRam - freeRam;
                float ramUsagePercent = (usedRam / totalRam) * 100;

                Dispatcher.Invoke(() =>
                {
                    CpuText.Text = $"{realCpu:F1} %";
                    CpuBar.Value = realCpu;
                    CpuBar.Foreground = new SolidColorBrush(GetStatusColor(realCpu));

                    RamText.Text = $"{usedRam:F0} MB / {totalRam:F0} MB";
                    RamBar.Value = ramUsagePercent;
                    RamBar.Foreground = new SolidColorBrush(GetStatusColor(ramUsagePercent));

                    if (realCpu > 85 && DateTime.Now - lastCpuAlertTime > TimeSpan.FromMinutes(alertCooldownMinutes))
                    {
                        ShowAlert("Vysoké zaťaženie CPU", $"CPU: {realCpu:F1} %");
                        WriteEventLog("CPU zaťaženie prekročilo 85 %", EventLogEntryType.Warning);
                        lastCpuAlertTime = DateTime.Now;
                    }

                    if (ramUsagePercent > 90 && DateTime.Now - lastRamAlertTime > TimeSpan.FromMinutes(alertCooldownMinutes))
                    {
                        ShowAlert("Vysoké využitie RAM", $"RAM: {usedRam:F0} MB / {totalRam:F0} MB");
                        WriteEventLog("RAM využitie prekročilo 90 %", EventLogEntryType.Warning);
                        lastRamAlertTime = DateTime.Now;
                    }
                });
            });
        }

        private void ShowAlert(string title, string message)
        {
            trayIcon.BalloonTipTitle = title;
            trayIcon.BalloonTipText = message;
            trayIcon.ShowBalloonTip(3000);
        }

        private void WriteEventLog(string message, EventLogEntryType type)
        {
            string source = "FilipPG";
            string log = "Application";

            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, log);
            }

            EventLog.WriteEntry(source, message, type);
        }

        private float GetTotalPhysicalMemoryInMB()
        {
            var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToUInt64(obj["TotalPhysicalMemory"]) / (1024f * 1024f);
            }
            return 0;
        }

        private MediaColor GetStatusColor(float percent)
        {
            if (percent < 50)
                return MediaColors.Green;
            if (percent < 80)
                return MediaColors.Orange;
            return MediaColors.Red;
        }

        private void SilenceFilip_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            await UpdateChecker.CheckForUpdatesAsync();
        }
    }
}
