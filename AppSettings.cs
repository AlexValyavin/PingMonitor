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

        public static void Save(AppSettings settings)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                using (TextWriter writer = new StreamWriter("settings.xml")) serializer.Serialize(writer, settings);
            }
            catch { }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists("settings.xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    using (TextReader reader = new StreamReader("settings.xml")) return (AppSettings)serializer.Deserialize(reader);
                }
            }
            catch { }
            return new AppSettings();
        }
    }
}