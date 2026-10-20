using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SitePhotoUploader
{
    public class Recorder
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")]
        private static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008; // Для правой кнопки
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;   // Для правой кнопки

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelMouseProc _proc;
        private bool _isRecording;
        private bool _isPlaying;
        private bool _isPaused = false;

        private bool _isWaitingForLinkClick = false;
        private Action<Point>? _onLinkCoordinatesCaptured;
        private System.Windows.Forms.Timer? _linkTimeoutTimer;

        private IntPtr _formHandle;
        private Control _uiControl;

        public event Action<Point> OnClickRecorded;
        public event Action<string> OnLog;
        public event Action<string> OnLinkCaptured;

        public bool IsRecording => _isRecording;
        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;

        public void SetFormHandle(IntPtr handle, Control control)
        {
            _formHandle = handle;
            _uiControl = control;
        }

        public void StartRecording()
        {
            if (_isRecording) return;
            _proc = HookCallback;
            _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, IntPtr.Zero, 0);
            _isRecording = true;
            _isPaused = false;
            OnLog?.Invoke("Запись кликов начата...");
        }

        public void StopRecording()
        {
            if (!_isRecording) return;
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _isRecording = false;
            _isPaused = false;
            _isWaitingForLinkClick = false;
            OnLog?.Invoke("Запись кликов остановлена.");
        }

        public void PauseRecording()
        {
            if (!_isRecording) return;
            _isPaused = true;
            OnLog?.Invoke("⏸ Запись на паузе.");
        }

        public void ResumeRecording()
        {
            if (!_isRecording) return;
            _isPaused = false;
            OnLog?.Invoke("▶ Запись продолжена.");
        }

        public void WaitForSingleClick(Action<Point> onCaptured)
        {
            _onLinkCoordinatesCaptured = onCaptured;
            _isWaitingForLinkClick = true;

            _linkTimeoutTimer?.Stop();
            _linkTimeoutTimer?.Dispose();
            _linkTimeoutTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            _linkTimeoutTimer.Tick += (s, e) =>
            {
                if (_isWaitingForLinkClick)
                {
                    _isWaitingForLinkClick = false;
                    _linkTimeoutTimer?.Stop();
                    OnLog?.Invoke("ОШИБКА: Время ожидания клика по ссылке истекло (10 сек).");
                }
            };
            _linkTimeoutTimer.Start();
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                IntPtr hWnd = WindowFromPoint(hookStruct.pt);
                bool isOurWindow = false;

                if (hWnd != IntPtr.Zero && _formHandle != IntPtr.Zero)
                {
                    isOurWindow = (hWnd == _formHandle) || IsChild(_formHandle, hWnd);
                }

                if (!isOurWindow)
                {
                    if (_isWaitingForLinkClick)
                    {
                        _isWaitingForLinkClick = false;
                        _linkTimeoutTimer?.Stop();
                        var clickPoint = new Point(hookStruct.pt.X, hookStruct.pt.Y);
                        _onLinkCoordinatesCaptured?.Invoke(clickPoint);
                        OnLog?.Invoke($"Координаты для ссылки сохранены: ({clickPoint.X}, {clickPoint.Y})");
                    }
                    else if (_isRecording && !_isPaused)
                    {
                        OnClickRecorded?.Invoke(new Point(hookStruct.pt.X, hookStruct.pt.Y));
                    }
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void PlaySteps(List<Step> steps, int imagesCount, FileManager fileManager, FileDialogHelper dialogHelper)
        {
            if (_isPlaying) return;

            _isPlaying = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    for (int imageIndex = 0; imageIndex < imagesCount; imageIndex++)
                    {
                        OnLog?.Invoke($"--- Картинка {imageIndex + 1} из {imagesCount} ---");

                        foreach (var step in steps)
                        {
                            if (!_isPlaying) return;

                            if (step.Type == StepType.Click)
                            {
                                ClickAt(step.Position);
                                Thread.Sleep(step.DelayMs);
                            }
                            else if (step.Type == StepType.OpenFileDialog)
                            {
                                Thread.Sleep(2000);
                                string path = fileManager.GetFile(imageIndex);
                                if (path == null) return;

                                bool ok = dialogHelper.SelectFileInOpenDialog(path);
                                OnLog?.Invoke(ok ? $"Вставлен путь: {System.IO.Path.GetFileName(path)}" : "Не удалось вставить путь");
                                Thread.Sleep(step.DelayMs);
                            }
                            else if (step.Type == StepType.CopyLink)
                            {
                                Thread.Sleep(2000);

                                string link = string.Empty;
                                for (int attempt = 1; attempt <= 3; attempt++)
                                {
                                    _uiControl?.Invoke(new Action(() =>
                                    {
                                        // 1. Находим окно Edge и принудительно активируем его
                                        IntPtr edgeHandle = FindWindow("Chrome_WidgetWin_1", null);
                                        if (edgeHandle != IntPtr.Zero) SetForegroundWindow(edgeHandle);
                                        Thread.Sleep(500);

                                        // 2. Кликаем ЛЕВОЙ кнопкой мыши (чтобы выделить)
                                        OnLog?.Invoke($"Выделяю текст ({step.ClickCount} клика)...");
                                        for (int i = 0; i < step.ClickCount; i++)
                                        {
                                            ClickAt(step.Position);
                                            Thread.Sleep(100);
                                        }
                                        Thread.Sleep(300);

                                        // 3. Кликаем ПРАВОЙ кнопкой мыши (вызываем контекстное меню)
                                        OnLog?.Invoke("Вызываю контекстное меню (ПКМ)...");
                                        RightClickAt(step.Position);
                                        Thread.Sleep(500); // Ждем открытия меню

                                        // 4. Нажимаем 'c' или Enter (обычно "Копировать" в меню)
                                        SendKeys.SendWait("c");
                                        Thread.Sleep(200);
                                        SendKeys.SendWait("{ENTER}"); // На всякий случай жмем Enter
                                        Thread.Sleep(300);

                                        // 5. Читаем буфер
                                        if (Clipboard.ContainsText())
                                        {
                                            link = Clipboard.GetText().Trim();
                                        }

                                        // Закрываем меню, если оно осталось открытым (Esc)
                                        SendKeys.SendWait("{ESC}");
                                    }));

                                    if (!string.IsNullOrEmpty(link)) break;

                                    OnLog?.Invoke($"Попытка {attempt}: не удалось, пробую еще...");
                                    Thread.Sleep(1000);
                                }

                                if (!string.IsNullOrEmpty(link))
                                {
                                    OnLog?.Invoke($"Скопирована ссылка: {link}");
                                    OnLinkCaptured?.Invoke(link);
                                }
                                else
                                {
                                    OnLog?.Invoke("ОШИБКА: не удалось скопировать ссылку.");
                                }

                                // Закрываем окно "Изображение" (нажимаем Esc)
                                Thread.Sleep(300);
                                _uiControl?.Invoke(new Action(() => SendKeys.SendWait("{ESC}")));
                                Thread.Sleep(step.DelayMs);
                            }
                        }
                    }
                }
                finally
                {
                    _isPlaying = false;
                    OnLog?.Invoke("=== ВСЕ КАРТИНКИ ЗАГРУЖЕНЫ! ===");

                    if (_uiControl != null && _uiControl.IsHandleCreated)
                    {
                        _uiControl.Invoke(new Action(() =>
                        {
                            var form = _uiControl.FindForm();
                            if (form != null)
                            {
                                form.WindowState = FormWindowState.Normal;
                                form.Activate();
                            }
                        }));
                    }
                }
            });
        }

        public void StopPlayback() => _isPlaying = false;

        public static void ClickAt(Point p)
        {
            SetCursorPos(p.X, p.Y);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        // Новый метод: клик правой кнопкой мыши
        public static void RightClickAt(Point p)
        {
            SetCursorPos(p.X, p.Y);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }
    }
}