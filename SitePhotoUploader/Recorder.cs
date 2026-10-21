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
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelMouseProc _proc;
        private bool _isRecording;
        private bool _isPlaying;
        private bool _isPaused = false;

        private int _waitingMode = 0;
        private Point _savedPoint = Point.Empty;
        private Action<Point>? _onLinkCaptured;
        private Action<Point, Point>? _onFileDialogCaptured;
        private System.Windows.Forms.Timer? _timeoutTimer;

        private IntPtr _formHandle;
        private Control _uiControl;

        public event Action<Point> OnClickRecorded;
        public event Action<string> OnLog;
        public event Action<string> OnLinkCapturedEvent;

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
            _waitingMode = 0;
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

        public void WaitForLinkClick(Action<Point> onCaptured)
        {
            _onLinkCaptured = onCaptured;
            _waitingMode = 1;
            StartTimeout(10);
        }

        public void WaitForFileDialogClicks(Action<Point, Point> onCaptured)
        {
            _onFileDialogCaptured = onCaptured;
            _waitingMode = 2;
            StartTimeout(20);
        }

        private void StartTimeout(int seconds)
        {
            _timeoutTimer?.Stop();
            _timeoutTimer?.Dispose();
            _timeoutTimer = new System.Windows.Forms.Timer { Interval = seconds * 1000 };
            _timeoutTimer.Tick += (s, e) =>
            {
                if (_waitingMode != 0)
                {
                    _waitingMode = 0;
                    _timeoutTimer?.Stop();
                    OnLog?.Invoke("ОШИБКА: Время ожидания кликов истекло.");
                }
            };
            _timeoutTimer.Start();
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
                    var clickPoint = new Point(hookStruct.pt.X, hookStruct.pt.Y);

                    if (_waitingMode == 1)
                    {
                        _waitingMode = 0;
                        _timeoutTimer?.Stop();
                        _onLinkCaptured?.Invoke(clickPoint);
                        OnLog?.Invoke($"Координаты ссылки: ({clickPoint.X}, {clickPoint.Y})");
                    }
                    else if (_waitingMode == 2)
                    {
                        _savedPoint = clickPoint;
                        _waitingMode = 3;
                        OnLog?.Invoke($"Точка ввода пути: ({clickPoint.X}, {clickPoint.Y}). Теперь кликните по кнопке 'Открыть'.");
                    }
                    else if (_waitingMode == 3)
                    {
                        _waitingMode = 0;
                        _timeoutTimer?.Stop();
                        _onFileDialogCaptured?.Invoke(_savedPoint, clickPoint);
                        OnLog?.Invoke($"Точка кнопки: ({clickPoint.X}, {clickPoint.Y}). Шаг добавлен.");
                    }
                    else if (_isRecording && !_isPaused)
                    {
                        OnClickRecorded?.Invoke(clickPoint);
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
                                Thread.Sleep(step.DelayMs);

                                string path = fileManager.GetFile(imageIndex);
                                if (path == null) return;

                                _uiControl?.Invoke(new Action(() =>
                                {
                                    // 1. Кликаем по полю ввода (по сохраненным координатам)
                                    ClickAt(step.InputPosition);
                                    Thread.Sleep(step.DelayMs);

                                    // 2. Вставляем путь напрямую (через WinAPI)
                                    dialogHelper.InsertPathIntoSaveDialog(path, step.DelayMs);

                                    Thread.Sleep(step.DelayMs);
                                    // 3. Кликаем по кнопке открытия
                                    ClickAt(step.OpenButtonPosition);
                                }));

                                OnLog?.Invoke($"Вставлен путь: {System.IO.Path.GetFileName(path)}");
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
                                        IntPtr edgeHandle = FindWindow("Chrome_WidgetWin_1", null);
                                        if (edgeHandle != IntPtr.Zero) SetForegroundWindow(edgeHandle);
                                        Thread.Sleep(500);

                                        for (int i = 0; i < step.ClickCount; i++)
                                        {
                                            ClickAt(step.Position);
                                            Thread.Sleep(100);
                                        }
                                        Thread.Sleep(300);

                                        RightClickAt(step.Position);
                                        Thread.Sleep(1000);

                                        for (int i = 0; i < step.DownArrowCount; i++)
                                        {
                                            SendKeys.SendWait("{DOWN}");
                                            Thread.Sleep(200);
                                        }

                                        SendKeys.SendWait("{ENTER}");
                                        Thread.Sleep(500);

                                        if (Clipboard.ContainsText())
                                        {
                                            link = Clipboard.GetText().Trim();
                                        }
                                        SendKeys.SendWait("{ESC}");
                                    }));

                                    if (!string.IsNullOrEmpty(link)) break;

                                    OnLog?.Invoke($"Попытка {attempt}: не удалось, пробую еще...");
                                    Thread.Sleep(1000);
                                }

                                if (!string.IsNullOrEmpty(link))
                                {
                                    OnLog?.Invoke($"Скопирована ссылка: {link}");
                                    OnLinkCapturedEvent?.Invoke(link);
                                }
                                else
                                {
                                    OnLog?.Invoke("ОШИБКА: не удалось скопировать ссылку.");
                                }

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