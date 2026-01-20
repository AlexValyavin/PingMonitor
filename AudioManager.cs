using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
//using System.Runtime.InteropServices;
//using System.Text;
using System.IO;

namespace PingMonitor
{
    public static class AudioManager
    {
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        // Метод для воспроизведения
        public static void PlaySound(string path, int volume0to100)
        {
            if (!File.Exists(path)) return;

            // Генерируем уникальное имя для алиаса, чтобы звуки не конфликтовали
            string alias = "sound_" + Guid.NewGuid().ToString().Replace("-", "");

            // 1. Открываем файл
            mciSendString($"open \"{path}\" type waveaudio alias {alias}", null, 0, IntPtr.Zero);

            // 2. Устанавливаем громкость (MCI использует шкалу 0-1000)
            int mciVolume = Math.Min(1000, Math.Max(0, volume0to100 * 10));
            mciSendString($"setaudio {alias} volume to {mciVolume}", null, 0, IntPtr.Zero);

            // 3. Играем
            mciSendString($"play {alias}", null, 0, IntPtr.Zero);

            // Важно: MCI сам не закрывает хендлы сразу, но для простого уведомления это ок.
            // Для идеала нужно делать close после окончания, но это усложнит код таймерами.
            // Windows сама почистит ресурсы при закрытии приложения.
        }
    }
}