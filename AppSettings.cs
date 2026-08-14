using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;

namespace PingMonitor
{
    public class AppSettings
    {
        // ... (Старые настройки звука оставляем) ...
        public bool LossAlertEnabled { get; set; } = true;
        public string LossSoundFile { get; set; } = @"C:\Windows\Media\Windows Critical Stop.wav";
        public int LossVolume { get; set; } = 100;
        public bool HighPingAlertEnabled { get; set; } = false;
        public string HighPingSoundFile { get; set; } = @"C:\Windows\Media\Windows Ding.wav";
        public int HighPingVolume { get; set; } = 50;
        public int HighPingThreshold { get; set; } = 200;

        // <--- НОВЫЕ НАСТРОЙКИ: СПИСОК ШАБЛОНОВ ---
        public List<string> IpTemplates { get; set; } = new List<string>();
        public int LastTemplateIndex { get; set; } = 0; // Чтобы помнить выбор
        // ------------------------------------------

        // <--- ЗАКРЕПЛЁННЫЕ ПЛИТКИ (восстанавливаются после перезапуска) ---
        public List<string> PinnedAddresses { get; set; } = new List<string>();
        public List<string> PinnedAliases { get; set; } = new List<string>();
        public List<int> PinnedStatsPeriods { get; set; } = new List<int>();
        // ---------------------------------------------------------------

        // <--- ТЕМА ---
        public bool IsDarkTheme { get; set; } = true;
        public bool IsRussian { get; set; } = true;
        public bool AutoStart { get; set; } = false;
        // ------------

        // <--- НАСТРОЙКИ ИНТЕРВАЛОВ ---
        public int PingIntervalMs { get; set; } = 1000;       // 200–60000 мс
        public int GraphWindowSec { get; set; } = 50;          // 10–600 сек (окно графика)
        public int StatsWindowSec { get; set; } = 600;         // 30–3600 сек (окно статистики Loss%)
        // -----------------------------

        // Путь к файлу настроек в %APPDATA%
        private static string SettingsPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PingMonitor", "settings.xml");

        private static string LegacySettingsPath => "settings.xml"; // Старый путь рядом с exe

        public static void Save(AppSettings settings)
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                using (TextWriter writer = new StreamWriter(SettingsPath, false, new System.Text.UTF8Encoding(false)))
                    serializer.Serialize(writer, settings);
            }
            catch { /* Логирование можно добавить позже */ }
        }

        public static AppSettings Load()
        {
            // 1. Пробуем новый путь (%APPDATA%)
            if (File.Exists(SettingsPath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    using (TextReader reader = new StreamReader(SettingsPath, new System.Text.UTF8Encoding(false)))
                        return (AppSettings)serializer.Deserialize(reader);
                }
                catch { }
            }

            // 2. Миграция со старого пути (рядом с exe)
            if (File.Exists(LegacySettingsPath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    using (TextReader reader = new StreamReader(LegacySettingsPath))
                    {
                        var settings = (AppSettings)serializer.Deserialize(reader);
                        // Сохраняем в новое место
                        Save(settings);
                        // Удаляем старый файл (опционально)
                        try { File.Delete(LegacySettingsPath); } catch { }
                        return settings;
                    }
                }
                catch { }
            }

            return new AppSettings();
        }
    }
}