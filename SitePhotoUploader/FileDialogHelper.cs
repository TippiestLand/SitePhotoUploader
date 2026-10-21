using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SitePhotoUploader
{
    public class FileDialogHelper
    {
        // WinAPI для прямой отправки текста
        private const int WM_SETTEXT = 0x000C;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        public void InsertPathIntoSaveDialog(string filePath, int delayMs)
        {
            try
            {
                // 1. Находим окно Проводника
                IntPtr dialogHandle = FindWindow("#32770", "Открыть");
                if (dialogHandle == IntPtr.Zero)
                    dialogHandle = FindWindow("#32770", "Open");

                if (dialogHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(dialogHandle);
                }

                Thread.Sleep(500);

                // 2. Отправляем путь в активное поле ввода через SendMessage
                // Получаем текущий фокус (обычно это поле "Имя файла")
                IntPtr focusedControl = GetFocus();

                if (focusedControl != IntPtr.Zero)
                {
                    // Прямая отправка текста в контрол
                    SendMessage(focusedControl, WM_SETTEXT, IntPtr.Zero, filePath);
                    Thread.Sleep(200);

                    // Нажимаем Enter, чтобы применить
                    SendMessage(focusedControl, WM_KEYDOWN, (IntPtr)0x0D, IntPtr.Zero);
                    SendMessage(focusedControl, WM_KEYUP, (IntPtr)0x0D, IntPtr.Zero);
                }
                else
                {
                    // Если фокус не найден, используем старый метод (но с задержкой)
                    Clipboard.SetText(filePath);
                    Thread.Sleep(300);
                    SendKeys.SendWait("^v");
                    Thread.Sleep(300);
                    SendKeys.SendWait("{ENTER}");
                }
            }
            catch { }
        }
    }
}