using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SitePhotoUploader
{
    public class FileDialogHelper
    {
        private const int WM_SETTEXT = 0x000C;
        private const int BM_CLICK = 0x00F5;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public bool SelectFileInOpenDialog(string filePath, int timeoutMs = 5000)
        {
            Thread.Sleep(2000);

            IntPtr dialogHandle = IntPtr.Zero;
            DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);

            while (DateTime.Now < deadline)
            {
                dialogHandle = FindWindow("#32770", "Open");
                if (dialogHandle == IntPtr.Zero) dialogHandle = FindWindow("#32770", "Открыть");
                if (dialogHandle != IntPtr.Zero) break;
                Thread.Sleep(500);
            }

            if (dialogHandle == IntPtr.Zero) return false;

            SetForegroundWindow(dialogHandle);
            Thread.Sleep(5000);

            try
            {
                IntPtr editHandle = FindEditControl(dialogHandle);
                if (editHandle != IntPtr.Zero)
                {
                    SendMessage(editHandle, WM_SETTEXT, IntPtr.Zero, filePath);
                    Thread.Sleep(2000);
                }
                else
                {
                    Clipboard.SetText(filePath);
                    SendKeys.SendWait("^v");
                    Thread.Sleep(2000);
                }

                IntPtr openButton = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", "&Open");
                if (openButton == IntPtr.Zero) openButton = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", "Открыть");

                if (openButton != IntPtr.Zero)
                {
                    SendMessage(openButton, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    SendMessage(dialogHandle, 0x0100, (IntPtr)0x0D, IntPtr.Zero);
                    SendMessage(dialogHandle, 0x0101, (IntPtr)0x0D, IntPtr.Zero);
                }
                return true;
            }
            catch { return false; }
        }

        private IntPtr FindEditControl(IntPtr parent)
        {
            IntPtr comboBoxEx = FindWindowEx(parent, IntPtr.Zero, "ComboBoxEx32", null);
            if (comboBoxEx != IntPtr.Zero)
            {
                IntPtr comboBox = FindWindowEx(comboBoxEx, IntPtr.Zero, "ComboBox", null);
                if (comboBox != IntPtr.Zero)
                {
                    IntPtr edit = FindWindowEx(comboBox, IntPtr.Zero, "Edit", null);
                    if (edit != IntPtr.Zero) return edit;
                }
            }
            IntPtr simpleEdit = FindWindowEx(parent, IntPtr.Zero, "Edit", null);
            if (simpleEdit != IntPtr.Zero) return simpleEdit;

            IntPtr directUI = FindWindowEx(parent, IntPtr.Zero, "DirectUIHWND", null);
            if (directUI != IntPtr.Zero)
            {
                IntPtr edit = FindWindowEx(directUI, IntPtr.Zero, "Edit", null);
                if (edit != IntPtr.Zero) return edit;
            }
            return IntPtr.Zero;
        }
    }
}