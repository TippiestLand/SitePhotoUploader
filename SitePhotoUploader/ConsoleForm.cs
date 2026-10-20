using System;
using System.Drawing;
using System.Windows.Forms;

namespace SitePhotoUploader
{
    public class ConsoleForm : Form
    {
        public RichTextBox RtbLog { get; private set; }

        public ConsoleForm()
        {
            this.Text = "Консоль выполнения";
            this.Size = new Size(500, 250);
            this.StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.TopMost = true;
            this.ShowInTaskbar = false;

            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(10, screen.Height - this.Height - 10);

            RtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(245, 245, 235),
                ForeColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.None
            };

            this.Controls.Add(RtbLog);
        }

        public void Log(string message)
        {
            // Защита: если окно уже закрыто или уничтожено, не пытаемся писать
            if (this.IsDisposed || RtbLog.IsDisposed) return;

            if (RtbLog.InvokeRequired)
            {
                try { RtbLog.Invoke(new Action<string>(Log), message); } catch { }
                return;
            }

            string time = $"[{DateTime.Now:HH:mm:ss}] ";
            bool isError = message.Contains("ОШИБКА", StringComparison.OrdinalIgnoreCase) || message.Contains("!!!");

            RtbLog.SelectionStart = RtbLog.TextLength;
            RtbLog.SelectionColor = Color.Gray;
            RtbLog.AppendText(time);

            RtbLog.SelectionStart = RtbLog.TextLength;
            RtbLog.SelectionColor = isError ? Color.Crimson : Color.FromArgb(40, 40, 40);
            RtbLog.AppendText(message + Environment.NewLine);

            RtbLog.ScrollToCaret();
        }

        public void SetLogText(string text)
        {
            if (this.IsDisposed || RtbLog.IsDisposed) return;
            if (RtbLog.InvokeRequired)
            {
                try { RtbLog.Invoke(new Action<string>(SetLogText), text); } catch { }
                return;
            }
            RtbLog.Text = text;
            RtbLog.SelectionStart = RtbLog.TextLength;
            RtbLog.ScrollToCaret();
        }
    }
}