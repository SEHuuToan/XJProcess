using System;
using System.IO;
using System.Xml.Serialization;
using XJProcess.modal;

namespace XJProcess.Services
{
    public static class SettingService
    {
        private static readonly string FolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Setting");
        private static readonly string FilePath = Path.Combine(FolderPath, "setting.xml");

        public static SettingModel LoadSettings()
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }
                if (!File.Exists(FilePath))
                {
                    var defaultConfig = new SettingModel();
                    SaveSettings(defaultConfig);
                    return defaultConfig;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(SettingModel));
                using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    return (SettingModel)serializer.Deserialize(fs) ?? new SettingModel();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi đọc file cấu hình");
                return new SettingModel();
            }
        }
        /// <summary>Lưu cấu hình hiện tại vào file XML.</summary>
        public static void SaveSettings(SettingModel config)
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(SettingModel));
                using (FileStream fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
                {
                    serializer.Serialize(fs, config);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi lưu file cấu hình");
            }
        }
    }
}