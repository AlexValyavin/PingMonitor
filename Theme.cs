using System.Drawing;

namespace PingMonitor
{
    /// <summary>Цветовая тема приложения. Поддерживает переключение Dark / Light.</summary>
    public static class Theme
    {
        public static bool IsDark { get; private set; } = true;

        // --- Фон ---
        public static Color BgWindow    { get; private set; }
        public static Color BgHeader    { get; private set; }
        public static Color BgTile      { get; private set; }
        public static Color BgInput     { get; private set; }
        public static Color BgHover     { get; private set; }
        public static Color BgGroup     { get; private set; }
        public static Color BgTabSel    { get; private set; }

        // --- Границы ---
        public static Color Border      { get; private set; }
        public static Color BorderLight { get; private set; }

        // --- Текст ---
        public static Color Text        { get; private set; }
        public static Color TextDim     { get; private set; }
        public static Color TextHint    { get; private set; }

        // --- Акцент ---
        public static Color Accent      { get; private set; }
        public static Color AccentHover { get; private set; }
        public static Color AccentText  { get; private set; }

        // --- Статусы ---
        public static Color Ok          { get; private set; }
        public static Color Warning     { get; private set; }
        public static Color Danger      { get; private set; }
        public static Color DangerSoft  { get; private set; }

        // --- Иконки ---
        public static Color Icon        { get; private set; }
        public static Color IconHover   { get; private set; }
        public static Color IconPinned  { get; private set; }

        static Theme() => SetDark();

        public static void SetDark()
        {
            IsDark = true;
            BgWindow    = Color.FromArgb(18, 18, 20);
            BgHeader    = Color.FromArgb(30, 30, 32);
            BgTile      = Color.FromArgb(35, 35, 38);
            BgInput     = Color.FromArgb(50, 50, 55);
            BgHover     = Color.FromArgb(55, 55, 60);
            BgGroup     = Color.FromArgb(28, 28, 30);
            BgTabSel    = Color.FromArgb(45, 45, 50);
            Border      = Color.FromArgb(60, 60, 65);
            BorderLight = Color.FromArgb(80, 80, 85);
            Text        = Color.FromArgb(225, 225, 230);
            TextDim     = Color.FromArgb(150, 150, 155);
            TextHint    = Color.FromArgb(110, 110, 115);
            Accent      = Color.FromArgb(0, 122, 204);
            AccentHover = Color.FromArgb(28, 148, 232);
            AccentText  = Color.White;
            Ok          = Color.FromArgb(46, 204, 113);
            Warning     = Color.FromArgb(241, 196, 15);
            Danger      = Color.FromArgb(231, 76, 60);
            DangerSoft  = Color.FromArgb(243, 156, 18);
            Icon        = Color.FromArgb(200, 200, 200);
            IconHover   = Color.White;
            IconPinned  = Color.FromArgb(46, 204, 113);
        }

        public static void SetLight()
        {
            IsDark = false;
            BgWindow    = Color.FromArgb(245, 245, 245);
            BgHeader    = Color.FromArgb(230, 230, 232);
            BgTile      = Color.White;
            BgInput     = Color.White;
            BgHover     = Color.FromArgb(235, 240, 250);
            BgGroup     = Color.FromArgb(240, 240, 242);
            BgTabSel    = Color.White;
            Border      = Color.FromArgb(200, 200, 205);
            BorderLight = Color.FromArgb(220, 220, 225);
            Text        = Color.FromArgb(30, 30, 35);
            TextDim     = Color.FromArgb(100, 100, 110);
            TextHint    = Color.FromArgb(140, 140, 145);
            Accent      = Color.FromArgb(0, 102, 180);
            AccentHover = Color.FromArgb(28, 130, 210);
            AccentText  = Color.White;
            Ok          = Color.FromArgb(39, 174, 96);
            Warning     = Color.FromArgb(230, 180, 0);
            Danger      = Color.FromArgb(200, 50, 40);
            DangerSoft  = Color.FromArgb(230, 126, 34);
            Icon        = Color.FromArgb(80, 80, 85);
            IconHover   = Color.FromArgb(30, 30, 35);
            IconPinned  = Color.FromArgb(39, 174, 96);
        }
    }
}