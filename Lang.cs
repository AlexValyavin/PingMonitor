using System.Collections.Generic;

namespace PingMonitor
{
    public static class Lang
    {
        public static bool IsRu { get; private set; } = true;

        private static readonly Dictionary<string, string> Ru = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> En = new Dictionary<string, string>();

        static Lang()
        {
            // === Form1 ===
            Ru["title"] = "PingMonitor";                      En["title"] = "PingMonitor";
            Ru["mode"] = "Режим / Шаблон";                    En["mode"] = "Mode / Template";
            Ru["ip_hint"] = "IP / ID";                         En["ip_hint"] = "IP / ID";
            Ru["name_hint"] = "Имя (Опц.)";                   En["name_hint"] = "Name (Opt.)";
            Ru["normal_input"] = "Обычный ввод (IP)";          En["normal_input"] = "Normal input (IP)";
            Ru["add"] = "Добавить";                            En["add"] = "Add";
            Ru["exit"] = "Выход";                              En["exit"] = "Exit";
            Ru["minimize"] = "Свернуть";                       En["minimize"] = "Minimize";
            Ru["pin_window"] = "Поверх всех окон";             En["pin_window"] = "Always on top";
            Ru["info"] = "Справка";                            En["info"] = "About";
            Ru["settings"] = "Настройки";                       En["settings"] = "Settings";
            Ru["search"] = "Поиск";                            En["search"] = "Search";

            // === PingTile ===
            Ru["monitoring_started"] = "Мониторинг запущен";    En["monitoring_started"] = "Monitoring started";
            Ru["connection_up"] = "✅ Связь восстановлена (UP)"; En["connection_up"] = "✅ Connection restored (UP)";
            Ru["connection_down"] = "⛔ Связь потеряна (DOWN)";  En["connection_down"] = "⛔ Connection lost (DOWN)";
            Ru["timeout"] = "TIMEOUT";                          En["timeout"] = "TIMEOUT";
            Ru["waiting"] = "Waiting...";                       En["waiting"] = "Waiting...";
            Ru["rename"] = "✏ Переименовать";                   En["rename"] = "✏ Rename";
            Ru["stats_period"] = "📊 Период статистики";         En["stats_period"] = "📊 Stats period";
            Ru["period_10m"] = "10 минут";                      En["period_10m"] = "10 minutes";
            Ru["period_30m"] = "30 минут";                      En["period_30m"] = "30 minutes";
            Ru["period_1h"] = "1 час";                          En["period_1h"] = "1 hour";
            Ru["period_3h"] = "3 часа";                         En["period_3h"] = "3 hours";
            Ru["period_6h"] = "6 часов";                        En["period_6h"] = "6 hours";
            Ru["period_all"] = "С начала запуска";              En["period_all"] = "Since start";
            Ru["log"] = "📄 Журнал событий";                    En["log"] = "📄 Event log";
            Ru["cmd_ping"] = "Открыть CMD (Ping -t)";           En["cmd_ping"] = "Open CMD (Ping -t)";
            Ru["tracert"] = "Trace Route";                      En["tracert"] = "Trace Route";
            Ru["show_graph"] = "Показывать график";             En["show_graph"] = "Show graph";
            Ru["copy_addr"] = "Копировать адрес";               En["copy_addr"] = "Copy address";
            Ru["ping_ms"] = "ms";                               En["ping_ms"] = "ms";
            Ru["loss"] = "Loss";                                En["loss"] = "Loss";

            // === SettingsForm ===
            Ru["settings_title"] = "Настройки";                 En["settings_title"] = "Settings";
            Ru["save"] = "Сохранить";                           En["save"] = "Save";
            Ru["cancel"] = "Отмена";                            En["cancel"] = "Cancel";
            Ru["theme_label"] = "Тема оформления:";             En["theme_label"] = "Theme:";
            Ru["theme_dark"] = "Тёмная";                        En["theme_dark"] = "Dark";
            Ru["theme_light"] = "Светлая";                      En["theme_light"] = "Light";
            Ru["tab_alerts"] = "Оповещения";                    En["tab_alerts"] = "Alerts";
            Ru["tab_templates"] = "Шаблоны IP";                 En["tab_templates"] = "IP Templates";
            Ru["tab_intervals"] = "Интервалы";                  En["tab_intervals"] = "Intervals";
            Ru["tab_system"] = "Система";                       En["tab_system"] = "System";
            Ru["loss_alert"] = "🔴 При потере связи";           En["loss_alert"] = "🔴 On connection loss";
            Ru["enable_sound"] = "Включить звук";               En["enable_sound"] = "Enable sound";
            Ru["sound"] = "Звук:";                              En["sound"] = "Sound:";
            Ru["volume"] = "Громкость:";                        En["volume"] = "Volume:";
            Ru["high_ping"] = "🟡 При высоком пинге";           En["high_ping"] = "🟡 On high ping";
            Ru["threshold_ms"] = "Порог (мс):";                  En["threshold_ms"] = "Threshold (ms):";
            Ru["template_hint"] = "Создайте маски. Символ '*' заменяет курсор.\nПримеры: 192.168.1.*  или  *.google.com";
            Ru["template_hint_en"] = "Create masks. '*' replaces the cursor.\nExamples: 192.168.1.*  or  *.google.com";
            Ru["new_template"] = "Новый шаблон:";               En["new_template"] = "New template:";
            Ru["add_template"] = "Добавить";                    En["add_template"] = "Add";
            Ru["delete_selected"] = "Удалить выбранный";        En["delete_selected"] = "Delete selected";
            Ru["template_needs_star"] = "Шаблон должен содержать '*'";
            Ru["template_needs_star_en"] = "Template must contain '*'";
            Ru["ping_period"] = "⏱ Период пинга";               En["ping_period"] = "⏱ Ping interval";
            Ru["ping_period_hint"] = "Как часто пинговать узел"; En["ping_period_hint"] = "How often to ping";
            Ru["graph_window"] = "📊 Окно графика";              En["graph_window"] = "📊 Graph window";
            Ru["graph_window_hint"] = "Сколько секунд истории показывать на графике";
            Ru["graph_window_hint_en"] = "How many seconds of history to show on graph";
            Ru["stats_window"] = "📈 Окно статистики";           En["stats_window"] = "📈 Stats window";
            Ru["stats_window_hint"] = "Период для расчёта Loss% и отчётов";
            Ru["stats_window_hint_en"] = "Period for Loss% calculation and reports";
            Ru["system_group"] = "⚙ Общее";                     En["system_group"] = "⚙ General";
            Ru["autostart"] = "Автозапуск при старте Windows";  En["autostart"] = "Auto-start with Windows";
            Ru["lang_label"] = "Язык:";                         En["lang_label"] = "Language:";
            Ru["lang_ru"] = "Русский";                          En["lang_ru"] = "Russian";
            Ru["lang_en"] = "English";                          En["lang_en"] = "English";
            Ru["play"] = "Play";                                En["play"] = "Play";
            Ru["ms_label"] = "мс";                              En["ms_label"] = "ms";
            Ru["sec_label"] = "сек";                            En["sec_label"] = "sec";

            // === InputDialog ===
            Ru["ok"] = "OK";                                    En["ok"] = "OK";
            Ru["input_cancel"] = "Отмена";                      En["input_cancel"] = "Cancel";
            Ru["rename_title"] = "Переименовать";               En["rename_title"] = "Rename";
            Ru["rename_prompt"] = "Введите новое имя для ";      En["rename_prompt"] = "Enter new name for ";

            // === LogForm ===
            Ru["log_title"] = "Журнал событий";                 En["log_title"] = "Event log";

            // === AboutForm ===
            Ru["about_title"] = "О программе";                  En["about_title"] = "About";
            Ru["about_name"] = "PingMonitor";                   En["about_name"] = "PingMonitor";
            Ru["about_version"] = "Версия 1.0";                 En["about_version"] = "Version 1.0";
            Ru["about_desc"] = "Компактный инструмент для мониторинга доступности сетевых узлов.";
            Ru["about_desc_en"] = "A compact tool for monitoring network host availability.";
            Ru["about_close"] = "Закрыть";                      En["about_close"] = "Close";
        }

        public static void SetRu(bool isRu)
        {
            IsRu = isRu;
        }

        public static string Get(string key)
        {
            var dict = IsRu ? Ru : En;
            return dict.TryGetValue(key, out string val) ? val : key;
        }

        /// <summary>Форматирование с подстановкой, как string.Format</summary>
        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }
    }
}