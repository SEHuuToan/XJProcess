using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XJProcess.Services;

namespace XJProcess.modal
{
    internal class Language
    {
        public string CurrentLanguage { get; private set; } = "vi";
        public Language()
        {
            var settings = SettingService.LoadSettings();
            if (settings != null && !string.IsNullOrEmpty(settings.Language))
            {
                CurrentLanguage = settings.Language;
            }
        }
        public void SetLanguage(string langCode)
        {
            CurrentLanguage = langCode;
            var settings = SettingService.LoadSettings() ?? new SettingModel();
            settings.Language = langCode;
            SettingService.SaveSettings(settings);
        }
    }
}
