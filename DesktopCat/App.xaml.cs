using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;

namespace DesktopCat
{
    public partial class App : System.Windows.Application
    {
        private System.Windows.Forms.NotifyIcon? trayIcon;
        private System.Windows.Forms.ToolStripMenuItem? startupMenuItem;
        private System.Windows.Forms.ToolStripMenuItem? idleTimeMenuItem;

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            Process currentProcess = Process.GetCurrentProcess();

            bool anotherInstanceRunning =
                Process
                    .GetProcessesByName(currentProcess.ProcessName)
                    .Any(p => p.Id != currentProcess.Id);

            if (anotherInstanceRunning)
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);

            MainWindow window = new MainWindow();

            MainWindow = window;

            // Kullanıcının daha önce seçtiği süreyi yükle
            int savedIdleSeconds = GetIdleTimeSetting();

            window.SetIdleThreshold(savedIdleSeconds);

            window.Show();

            CreateTrayIcon(window, savedIdleSeconds);
        }

        private void CreateTrayIcon(
            MainWindow window,
            int currentIdleSeconds)
        {
            trayIcon =
                new System.Windows.Forms.NotifyIcon();

            trayIcon.Text = "Omletsu";

            // EXE'nin Omletsu ikonunu kullan
            string? exePath = Environment.ProcessPath;

            if (!string.IsNullOrWhiteSpace(exePath))
            {
                trayIcon.Icon =
                    System.Drawing.Icon.ExtractAssociatedIcon(exePath)
                    ?? System.Drawing.SystemIcons.Application;
            }
            else
            {
                trayIcon.Icon =
                    System.Drawing.SystemIcons.Application;
            }

            trayIcon.Visible = true;

            System.Windows.Forms.ContextMenuStrip menu =
                new System.Windows.Forms.ContextMenuStrip();


            // =========================
            // GÖSTER
            // =========================

            var showItem =
                new System.Windows.Forms.ToolStripMenuItem(
                    "Omletsu'yu Göster"
                );

            showItem.Click += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    window.ShowPetManually();
                });
            };


            // =========================
            // GİZLE
            // =========================

            var hideItem =
                new System.Windows.Forms.ToolStripMenuItem(
                    "Omletsu'yu Gizle"
                );

            hideItem.Click += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    window.HidePetManually();
                });
            };


            // =========================
            // OTOMATİK MOD
            // =========================

            var autoItem =
                new System.Windows.Forms.ToolStripMenuItem(
                    "Otomatik Mod"
                );

            autoItem.Click += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    window.EnableAutomaticMode();
                });
            };


            // =========================
            // BEKLEME SÜRESİ
            // =========================

            idleTimeMenuItem =
                new System.Windows.Forms.ToolStripMenuItem(
                    "Bekleme Süresi"
                );

            AddIdleOption(
                window,
                idleTimeMenuItem,
                "30 saniye",
                30,
                currentIdleSeconds
            );

            AddIdleOption(
                window,
                idleTimeMenuItem,
                "1 dakika",
                60,
                currentIdleSeconds
            );

            AddIdleOption(
                window,
                idleTimeMenuItem,
                "2 dakika",
                120,
                currentIdleSeconds
            );

            AddIdleOption(
                window,
                idleTimeMenuItem,
                "3 dakika",
                180,
                currentIdleSeconds
            );

            AddIdleOption(
                window,
                idleTimeMenuItem,
                "4 dakika",
                240,
                currentIdleSeconds
            );

            AddIdleOption(
                window,
                idleTimeMenuItem,
                "5 dakika",
                300,
                currentIdleSeconds
            );


            // =========================
            // WINDOWS İLE BAŞLAT
            // =========================

            startupMenuItem =
                new System.Windows.Forms.ToolStripMenuItem(
                    "Windows ile Başlat"
                );

            startupMenuItem.Checked =
                IsStartupEnabled();

            startupMenuItem.CheckOnClick = true;

            startupMenuItem.Click +=
                StartupMenuItem_Click;


            // =========================
            // ÇIKIŞ
            // =========================

            var exitItem =
                new System.Windows.Forms.ToolStripMenuItem(
                    "Çıkış"
                );

            exitItem.Click += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (trayIcon != null)
                    {
                        trayIcon.Visible = false;
                        trayIcon.Dispose();
                    }

                    Shutdown();
                });
            };


            // =========================
            // MENÜ
            // =========================

            menu.Items.Add(showItem);
            menu.Items.Add(hideItem);
            menu.Items.Add(autoItem);

            menu.Items.Add(
                new System.Windows.Forms.ToolStripSeparator()
            );

            menu.Items.Add(idleTimeMenuItem);

            menu.Items.Add(
                new System.Windows.Forms.ToolStripSeparator()
            );

            menu.Items.Add(startupMenuItem);

            menu.Items.Add(
                new System.Windows.Forms.ToolStripSeparator()
            );

            menu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = menu;


            // Tray ikonuna çift tıklayınca Omletsu'yu göster
            trayIcon.DoubleClick += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    window.ShowPetManually();
                });
            };
        }


        // =============================
        // BEKLEME SÜRESİ SEÇENEĞİ EKLE
        // =============================

        private void AddIdleOption(
            MainWindow window,
            System.Windows.Forms.ToolStripMenuItem parentMenu,
            string text,
            int seconds,
            int currentSeconds)
        {
            var item =
                new System.Windows.Forms.ToolStripMenuItem(text);

            item.Checked =
                seconds == currentSeconds;

            item.Click += (sender, args) =>
            {
                // Önce bütün seçeneklerin tikini kaldır
                foreach (
                    System.Windows.Forms.ToolStripItem menuItem
                    in parentMenu.DropDownItems)
                {
                    if (
                        menuItem
                        is System.Windows.Forms.ToolStripMenuItem option)
                    {
                        option.Checked = false;
                    }
                }

                // Seçilen seçeneği tikle
                item.Checked = true;

                Dispatcher.Invoke(() =>
                {
                    window.SetIdleThreshold(seconds);
                });

                SaveIdleTimeSetting(seconds);
            };

            parentMenu.DropDownItems.Add(item);
        }


        // =============================
        // BEKLEME SÜRESİNİ KAYDET
        // =============================

        private void SaveIdleTimeSetting(int seconds)
        {
            using RegistryKey key =
                Registry.CurrentUser.CreateSubKey(
                    @"Software\DesktopCat"
                );

            key.SetValue(
                "IdleSeconds",
                seconds,
                RegistryValueKind.DWord
            );
        }


        // =============================
        // KAYITLI SÜREYİ OKU
        // =============================

        private int GetIdleTimeSetting()
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    @"Software\DesktopCat"
                );

            object? value =
                key?.GetValue("IdleSeconds");

            if (
                value != null &&
                int.TryParse(
                    value.ToString(),
                    out int seconds
                )
            )
            {
                return seconds;
            }

            // İlk kurulumda varsayılan 2 dakika
            return 120;
        }


        // =============================
        // WINDOWS İLE BAŞLAT
        // =============================

        private void StartupMenuItem_Click(
            object? sender,
            EventArgs e)
        {
            if (startupMenuItem == null)
                return;

            if (startupMenuItem.Checked)
            {
                EnableStartup();
            }
            else
            {
                DisableStartup();
            }
        }

        private void EnableStartup()
        {
            string? exePath =
                Environment.ProcessPath;

            if (string.IsNullOrWhiteSpace(exePath))
                return;

            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    true
                );

            key?.SetValue(
                "DesktopCat",
                $"\"{exePath}\""
            );
        }

        private void DisableStartup()
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    true
                );

            key?.DeleteValue(
                "DesktopCat",
                false
            );
        }

        private bool IsStartupEnabled()
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run"
                );

            object? value =
                key?.GetValue(
                    "DesktopCat"
                );

            return value != null;
        }


        // =============================
        // KAPANIŞ
        // =============================

        protected override void OnExit(
            System.Windows.ExitEventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

            base.OnExit(e);
        }
    }
}