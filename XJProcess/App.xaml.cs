using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using XJProcess.services;
using XJProcess.Services;

namespace XJProcess
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private const string MutexUniqueName = "XJProcess_SingleInstance_Mutex_Key";

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _mutex = new Mutex(true, MutexUniqueName, out createdNew);

            if (!createdNew)
            {
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string localVersionFile = Path.Combine(baseDir, "version_local.txt");
                string localVersion;

                if (!File.Exists(localVersionFile))
                {
                    localVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
                    File.WriteAllText(localVersionFile, localVersion);
                }
                else
                {
                    localVersion = File.ReadAllText(localVersionFile).Trim();
                }
                //bool hasUpdate = MainService.AutoUpdater.CheckFormUpdate(localVersion);
                bool hasUpdate = false;
                if (hasUpdate)
                {
                    string updaterPath = Path.Combine(baseDir, "UpdaterXJProcess.exe");
                    if (File.Exists(updaterPath))
                    {
                        System.Diagnostics.Process.Start(updaterPath);
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy file UpdaterXJProcess.exe để cập nhật!", "Lỗi cập nhật", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Current.Shutdown();
                    return;
                }
                var settings = SettingService.LoadSettings();
                if (settings != null && !string.IsNullOrEmpty(settings.Language))
                {
                    MainService.ChangeAppLanguage(settings.Language);
                }
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động ứng dụng: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }

        }

        
    }
}