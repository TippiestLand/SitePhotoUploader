using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SitePhotoUploader
{
    public partial class Form1 : Form
    {
        // ... поля остаются такими же, как в прошлом сообщении ...
        // (Для экономии места я не дублирую все объявления полей, они не менялись)

        private TextBox txtFolder = null!;
        private Button btnChooseFolder = null!;
        private Label lblCount = null!;
        private ListBox lstSteps = null!;
        private RichTextBox rtbLog = null!;

        private Button btnRecordStart = null!;
        private Button btnRecordStop = null!;
        private Button btnPauseRecord = null!;
        private Button btnAddOpenFile = null!;
        private Button btnClearSteps = null!;

        private Button btnAddCopyLink = null!;
        private Button btnCopyLinkMenu = null!;
        private ContextMenuStrip contextMenuClicks = null!;
        private int _selectedClicks = 3;

        private Button btnPlayStart = null!;
        private Button btnPlayStop = null!;
        private Button btnSaveLinks = null!;
        private Button btnClearLinks = null!;

        private NumericUpDown numDelaySec = null!;
        private Label lblEstTime = null!;

        private Button btnClearLog = null!;
        private Button btnCopyLog = null!;
        private Button btnSaveLog = null!;

        private Panel pnlTemplates = null!;
        private Button btnToggleTemplates = null!;
        private ComboBox cmbTemplates = null!;
        private Button btnLoadTemplate = null!;
        private Button btnSaveTemplate = null!;
        private Button btnDeleteTemplate = null!;
        private bool _templatesOpen = false;

        private Panel pnlHelp = null!;
        private Button btnToggleHelp = null!;
        private Panel mainContentPanel = null!;
        private bool _helpOpen = false;

        private FileManager _fileManager = new FileManager();
        private Recorder _recorder = new Recorder();
        private FileDialogHelper _dialogHelper = new FileDialogHelper();
        private TemplateManager _templateManager = new TemplateManager();
        private List<TemplateData> _templates = new List<TemplateData>();

        private List<Step> _steps = new List<Step>();
        private List<string> _links = new List<string>();

        private ConsoleForm? _consoleForm;

        public Form1()
        {
            InitializeComponent();
            BuildUi();

            _recorder.SetFormHandle(this.Handle, this);

            _recorder.OnClickRecorded += pos =>
            {
                AddStep(new Step { Type = StepType.Click, Position = pos, DelayMs = (int)numDelaySec.Value * 1000 });
                Log($"Записан клик: ({pos.X}, {pos.Y})");
            };

            _recorder.OnLog += Log;
            _recorder.OnLinkCaptured += link =>
            {
                if (!_links.Contains(link))
                {
                    _links.Add(link);
                    Log($"Ссылка добавлена. Всего: {_links.Count}");
                }
            };

            LoadTemplatesToList();
        }

        private void BuildUi()
        {
            // ... Код BuildUi остается без изменений, как в предыдущем сообщении ...
            // Я привожу его полностью, чтобы вы могли просто скопировать.

            this.Text = "Site Photo Uploader";
            this.Size = new Size(1350, 800);
            this.MinimumSize = new Size(1150, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10);

            mainContentPanel = new Panel { Dock = DockStyle.Fill };

            var top = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };

            btnChooseFolder = CreateButton("Выбрать папку", 8, 8, 200, 30);
            btnChooseFolder.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        txtFolder.Text = dlg.SelectedPath;
                        int count = _fileManager.LoadFolder(dlg.SelectedPath);
                        lblCount.Text = $"Найдено: {count}";
                        UpdateEstimate();
                    }
                }
            };

            txtFolder = new TextBox { Location = new Point(215, 12), Width = mainContentPanel.Width - 400, ReadOnly = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            lblCount = new Label { Location = new Point(1100, 15), Width = 100, TextAlign = ContentAlignment.MiddleLeft, Text = "Найдено: 0", Anchor = AnchorStyles.Top | AnchorStyles.Right };

            top.Controls.Add(btnChooseFolder);
            top.Controls.Add(txtFolder);
            top.Controls.Add(lblCount);

            var controlPanel = new Panel { Dock = DockStyle.Top, Height = 160, Padding = new Padding(8) };

            var lblRecordGroup = CreateLabel("ЗАПИСЬ", 8, 5, Color.Gray, 8, FontStyle.Bold);

            btnRecordStart = CreateButton("● Начать запись", 8, 23, 140, 32);
            btnRecordStart.Click += (s, e) => { _recorder.StartRecording(); Log("Запись начата..."); };

            btnRecordStop = CreateButton("■ Стоп запись", 153, 23, 140, 32);
            btnRecordStop.Click += (s, e) => { _recorder.StopRecording(); Log("Запись остановлена."); };

            btnPauseRecord = CreateButton("⏸ Пауза записи", 298, 23, 140, 32);
            btnPauseRecord.Click += (s, e) =>
            {
                if (_recorder.IsPaused)
                {
                    _recorder.ResumeRecording();
                    btnPauseRecord.Text = "⏸ Пауза записи";
                }
                else
                {
                    _recorder.PauseRecording();
                    btnPauseRecord.Text = "▶ Продолжить";
                }
            };

            btnAddOpenFile = CreateButton("+ Открыть файл", 443, 23, 140, 32);
            btnAddOpenFile.Click += (s, e) => { AddStep(new Step { Type = StepType.OpenFileDialog }); Log("Добавлен шаг: открытие файла"); };

            contextMenuClicks = new ContextMenuStrip();
            contextMenuClicks.Items.Add("1 клик", null, (s, e) => SetClicks(1));
            contextMenuClicks.Items.Add("2 клика", null, (s, e) => SetClicks(2));
            contextMenuClicks.Items.Add("3 клика", null, (s, e) => SetClicks(3));
            contextMenuClicks.Items.Add("4 клика", null, (s, e) => SetClicks(4));

            btnAddCopyLink = CreateButton("+ Копировать ссылку", 588, 23, 150, 32);
            btnAddCopyLink.Click += BtnAddCopyLink_Click;

            btnCopyLinkMenu = CreateButton("▼", 738, 23, 30, 32);
            btnCopyLinkMenu.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            btnCopyLinkMenu.Click += (s, e) =>
            {
                contextMenuClicks.Show(btnCopyLinkMenu, new Point(0, btnCopyLinkMenu.Height));
            };

            btnClearSteps = CreateButton("Очистить шаги", 785, 23, 120, 32);
            btnClearSteps.ForeColor = Color.DarkRed;
            btnClearSteps.Click += (s, e) =>
            {
                _steps.Clear();
                UpdateStepsList();
                UpdateEstimate();
                Log("Список шагов очищен.");
            };

            var lblDelay = CreateLabel("Задержка (сек):", 920, 28, Color.Empty, 10, FontStyle.Regular);
            numDelaySec = new NumericUpDown { Location = new Point(1035, 25), Width = 60, Minimum = 1, Maximum = 60, Value = 10 };
            numDelaySec.ValueChanged += (s, e) =>
            {
                foreach (var step in _steps) step.DelayMs = (int)numDelaySec.Value * 1000;
                UpdateStepsList();
                UpdateEstimate();
            };

            var lblPlayGroup = CreateLabel("ЗАПУСК", 8, 85, Color.Gray, 8, FontStyle.Bold);

            btnPlayStart = CreateButton("▶ Начать цикл", 8, 103, 140, 32);
            btnPlayStart.Click += (s, e) =>
            {
                if (_fileManager.Count == 0) { Log("ОШИБКА: Выберите папку с картинками."); return; }
                if (_steps.Count == 0) { Log("ОШИБКА: Нет записанных шагов."); return; }

                _links.Clear();
                Log("=== Запуск цикла ===");

                if (_consoleForm == null || _consoleForm.IsDisposed)
                {
                    _consoleForm = new ConsoleForm();
                    _consoleForm.SetLogText(rtbLog.Text);
                    _consoleForm.Show();
                }

                this.WindowState = FormWindowState.Minimized;
                _recorder.PlaySteps(_steps, _fileManager.Count, _fileManager, _dialogHelper);
            };

            btnPlayStop = CreateButton("■ Остановить", 153, 103, 140, 32);
            btnPlayStop.Click += (s, e) => { _recorder.StopPlayback(); _consoleForm?.Close(); };

            btnSaveLinks = CreateButton("Сохранить ссылки в TXT", 298, 103, 200, 32);
            btnSaveLinks.Click += (s, e) => SaveLinksToTxt();

            btnClearLinks = CreateButton("Очистить ссылки", 503, 103, 130, 32);
            btnClearLinks.ForeColor = Color.DarkRed;
            btnClearLinks.Click += (s, e) => { _links.Clear(); Log("Список ссылок очищен."); };

            lblEstTime = new Label
            {
                Text = "Примерное время: 0 сек",
                Location = new Point(650, 108),
                Width = 400,
                ForeColor = Color.DarkBlue,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            controlPanel.Controls.AddRange(new Control[] {
                lblRecordGroup, btnRecordStart, btnRecordStop, btnPauseRecord, btnAddOpenFile, btnAddCopyLink, btnCopyLinkMenu, btnClearSteps,
                lblDelay, numDelaySec, lblPlayGroup, btnPlayStart, btnPlayStop, btnSaveLinks, btnClearLinks, lblEstTime
            });

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 200 };
            lstSteps = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };

            var consolePanel = new Panel { Dock = DockStyle.Fill };
            var logToolsPanel = new Panel { Dock = DockStyle.Top, Height = 30 };

            btnClearLog = CreateButton("Очистить лог", 0, 0, 120, 25); btnClearLog.Dock = DockStyle.Left; btnClearLog.Font = new Font("Segoe UI", 8);
            btnClearLog.Click += (s, e) => rtbLog.Clear();

            btnCopyLog = CreateButton("Копировать лог", 0, 0, 120, 25); btnCopyLog.Dock = DockStyle.Left; btnCopyLog.Font = new Font("Segoe UI", 8);
            btnCopyLog.Click += (s, e) => { if (!string.IsNullOrEmpty(rtbLog.Text)) Clipboard.SetText(rtbLog.Text); };

            btnSaveLog = CreateButton("Сохранить лог", 0, 0, 120, 25); btnSaveLog.Dock = DockStyle.Left; btnSaveLog.Font = new Font("Segoe UI", 8);
            btnSaveLog.Click += (s, e) => SaveLogToTxt();

            logToolsPanel.Controls.AddRange(new Control[] { btnSaveLog, btnCopyLog, btnClearLog });

            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5f),
                BackColor = Color.FromArgb(245, 245, 235),
                ForeColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.None
            };

            consolePanel.Controls.Add(rtbLog);
            consolePanel.Controls.Add(logToolsPanel);

            split.Panel1.Controls.Add(lstSteps);
            split.Panel2.Controls.Add(consolePanel);

            mainContentPanel.Controls.Add(split);
            mainContentPanel.Controls.Add(controlPanel);
            mainContentPanel.Controls.Add(top);

            btnToggleTemplates = new Button { Text = "▶", Dock = DockStyle.Left, Width = 28, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.FromArgb(230, 230, 230), Cursor = Cursors.Hand };
            btnToggleTemplates.FlatAppearance.BorderSize = 0;
            btnToggleTemplates.Click += (s, e) => ToggleTemplates();

            pnlTemplates = new Panel { Dock = DockStyle.Left, Width = 0, BackColor = Color.FromArgb(248, 248, 250), Padding = new Padding(12), Visible = true };

            var lblTemplatesTitle = new Label { Text = "Шаблоны", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            btnSaveTemplate = new Button { Text = "Сохранить текущий", Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 5, 0, 5) };
            btnSaveTemplate.Click += (s, e) => ShowSaveTemplateDialog();

            var spacer = new Panel { Dock = DockStyle.Top, Height = 15 };
            cmbTemplates = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };

            btnLoadTemplate = new Button { Text = "Загрузить шаблон", Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 5, 0, 5) };
            btnLoadTemplate.Click += (s, e) =>
            {
                if (cmbTemplates.SelectedIndex < 0) return;
                var tmpl = _templates[cmbTemplates.SelectedIndex];
                _steps.Clear();
                foreach (var step in tmpl.Steps)
                {
                    _steps.Add(new Step { Type = step.Type, Position = step.Position, DelayMs = step.DelayMs, ClickCount = step.ClickCount });
                }

                if (_steps.Count > 0) numDelaySec.Value = _steps[0].DelayMs / 1000;

                UpdateStepsList();
                UpdateEstimate();
                Log($"Загружен шаблон: {tmpl.Name} ({tmpl.Steps.Count} шагов)");
            };

            btnDeleteTemplate = new Button { Text = "Удалить шаблон", Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 5, 0, 5), ForeColor = Color.DarkRed };
            btnDeleteTemplate.Click += (s, e) => DeleteSelectedTemplate();

            pnlTemplates.Controls.AddRange(new Control[] { btnLoadTemplate, btnDeleteTemplate, spacer, cmbTemplates, btnSaveTemplate, lblTemplatesTitle });

            btnToggleHelp = new Button { Text = "◀", Dock = DockStyle.Right, Width = 28, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.FromArgb(230, 230, 230), Cursor = Cursors.Hand };
            btnToggleHelp.FlatAppearance.BorderSize = 0;
            btnToggleHelp.Click += (s, e) => ToggleHelp();

            pnlHelp = new Panel { Dock = DockStyle.Right, Width = 0, BackColor = Color.FromArgb(250, 250, 252), Padding = new Padding(15), Visible = true, AutoScroll = true };

            var webHelp = new WebBrowser
            {
                Dock = DockStyle.Fill,
                ScrollBarsEnabled = true,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = false,
                AllowWebBrowserDrop = false,
                DocumentText = @"
                    <html>
                    <head>
                        <style>
                            body { font-family: 'Segoe UI', Arial; font-size: 13px; color: #333; margin: 0; padding: 5px; }
                            h2 { font-size: 16px; color: #005c51; border-bottom: 1px solid #ddd; padding-bottom: 5px; margin-top: 15px; }
                            h3 { font-size: 14px; color: #444; margin-bottom: 5px; }
                            ul { padding-left: 20px; margin-top: 5px; }
                            li { margin-bottom: 6px; line-height: 1.4; }
                            .num { font-weight: bold; color: #0078d7; }
                            .warn { background: #fff3cd; padding: 8px; border-radius: 4px; border-left: 4px solid #ffc107; font-size: 12px; }
                            .tip { background: #d1ecf1; padding: 8px; border-radius: 4px; border-left: 4px solid #17a2b8; font-size: 12px; }
                        </style>
                    </head>
                    <body>
                        <h2>🚀 Базовый сценарий</h2>
                        <ol>
                            <li>Выберите папку с картинками.</li>
                            <li>Нажмите <b>«● Начать запись»</b>.</li>
                            <li>Кликайте по сайту до кнопки выбора файла.</li>
                            <li>Кликните «Choose File» (откроется Проводник).</li>
                            <li>В программе нажмите <b>«⏸ Пауза записи»</b>.</li>
                            <li>Выберите файл и нажмите «Открыть».</li>
                            <li>Вернитесь в программу и нажмите <b>«▶ Продолжить»</b>.</li>
                            <li>Нажмите <b>«+ Открыть файл»</b>.</li>
                            <li>На сайте нажмите «Сохранить».</li>
                            <li>Когда появится окно с выделенной ссылкой, нажмите <b>«⏸ Пауза записи»</b>.</li>
                            <li>Нажмите <b>«+ Копировать ссылку»</b> и <b>один раз кликните по самой ссылке</b> в браузере (программа запишет это место).</li>
                            <li>Стрелочкой <b>▼</b> настройте, сколько раз программа кликнет по ссылке (1-4).</li>
                            <li>Нажмите <b>«■ Стоп запись»</b>.</li>
                            <li>Задайте задержку.</li>
                            <li>Нажмите <b>«▶ Начать цикл»</b>.</li>
                            <li>После завершения — <b>«Сохранить ссылки в TXT»</b>.</li>
                        </ol>
                    </body>
                    </html>"
            };

            pnlHelp.Controls.Add(webHelp);

            this.Controls.Add(mainContentPanel);
            this.Controls.Add(pnlHelp);
            this.Controls.Add(btnToggleHelp);
            this.Controls.Add(pnlTemplates);
            this.Controls.Add(btnToggleTemplates);
        }

        private void BtnAddCopyLink_Click(object? sender, EventArgs e)
        {
            Log($"Ожидание клика по ссылке. Кликните в браузере (программа кликнет {_selectedClicks} раз(а)).");

            _recorder.WaitForSingleClick((clickPos) =>
            {
                AddStep(new Step { Type = StepType.CopyLink, Position = clickPos, ClickCount = _selectedClicks });
                Log($"Координаты для ссылки сохранены: ({clickPos.X}, {clickPos.Y}). Шаг добавлен.");
            });
        }

        private void SetClicks(int count)
        {
            _selectedClicks = count;
            btnCopyLinkMenu.Text = count.ToString();
            Log($"Настройка изменена: программа будет кликать {_selectedClicks} раз(а) по ссылке.");
        }

        private void DeleteSelectedTemplate()
        {
            if (cmbTemplates.SelectedIndex < 0)
            {
                MessageBox.Show("Сначала выберите шаблон из списка.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string templateName = cmbTemplates.SelectedItem.ToString()!;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить шаблон «{templateName}»?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var templateToRemove = _templates.FirstOrDefault(t => t.Name == templateName);
                if (templateToRemove != null)
                {
                    _templates.Remove(templateToRemove);
                    _templateManager.SaveTemplates(_templates);
                    LoadTemplatesToList();
                    Log($"Шаблон «{templateName}» успешно удален.");
                }
            }
        }

        // ===== Вспомогательные методы =====
        private Button CreateButton(string text, int x, int y, int w, int h)
        {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h) };
        }

        private Label CreateLabel(string text, int x, int y, Color color, int size, FontStyle style)
        {
            return new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = color, Font = new Font("Segoe UI", size, style) };
        }

        private void AddStep(Step step)
        {
            step.DelayMs = (int)numDelaySec.Value * 1000;
            _steps.Add(step);
            UpdateStepsList();
            UpdateEstimate();
        }

        private void SaveLogToTxt()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string path = Path.Combine(desktop, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(path, rtbLog.Text);
                Log($"Лог сохранен: {path}");
            }
            catch (Exception ex) { Log($"Ошибка сохранения лога: {ex.Message}"); }
        }

        private void UpdateEstimate()
        {
            if (lblEstTime == null) return;
            int totalSeconds = _fileManager.Count * _steps.Count * ((int)numDelaySec.Value + 4);
            if (totalSeconds == 0) { lblEstTime.Text = "Примерное время: 0 сек"; return; }

            TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
            lblEstTime.Text = time.TotalHours >= 1
                ? $"Примерное время: {(int)time.TotalHours} ч {time.Minutes} мин"
                : $"Примерное время: {time.Minutes} мин {time.Seconds} сек";
        }

        private void ToggleTemplates()
        {
            _templatesOpen = !_templatesOpen;
            pnlTemplates.Width = _templatesOpen ? 250 : 0;
            btnToggleTemplates.Text = _templatesOpen ? "◀" : "▶";
        }

        private void ToggleHelp()
        {
            _helpOpen = !_helpOpen;
            pnlHelp.Width = _helpOpen ? 380 : 0;
            btnToggleHelp.Text = _helpOpen ? "▶" : "◀";
        }

        private void LoadTemplatesToList()
        {
            _templates = _templateManager.LoadTemplates();
            cmbTemplates.Items.Clear();
            foreach (var tmpl in _templates) cmbTemplates.Items.Add(tmpl.Name);
            if (cmbTemplates.Items.Count > 0) cmbTemplates.SelectedIndex = 0;
        }

        private void ShowSaveTemplateDialog()
        {
            if (_steps.Count == 0) { MessageBox.Show("Нет шагов для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            using (var form = new Form())
            {
                form.Text = "Сохранить шаблон";
                form.Size = new Size(350, 180);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false; form.MinimizeBox = false;

                var lbl = new Label { Text = "Введите название шаблона:", Location = new Point(15, 15), Width = 300 };
                var txt = new TextBox { Location = new Point(15, 45), Width = 300 };
                var btnOk = new Button { Text = "Сохранить", Location = new Point(150, 90), DialogResult = DialogResult.OK, Width = 100 };

                btnOk.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txt.Text)) { MessageBox.Show("Введите название."); form.DialogResult = DialogResult.None; return; }
                    if (_templates.Any(t => t.Name.Equals(txt.Text, StringComparison.OrdinalIgnoreCase))) { MessageBox.Show("Шаблон с таким именем уже есть."); form.DialogResult = DialogResult.None; return; }
                };

                form.Controls.Add(lbl); form.Controls.Add(txt); form.Controls.Add(btnOk);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    var newTemplate = new TemplateData { Name = txt.Text.Trim(), Steps = _steps.Select(s => new Step { Type = s.Type, Position = s.Position, DelayMs = s.DelayMs, ClickCount = s.ClickCount }).ToList() };
                    _templates.Add(newTemplate);
                    _templateManager.SaveTemplates(_templates);
                    LoadTemplatesToList();
                    Log($"Шаблон «{newTemplate.Name}» сохранен.");
                }
            }
        }

        private void SaveLinksToTxt()
        {
            if (_links.Count == 0) { Log("ОШИБКА: Нет ссылок для сохранения."); return; }
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string path = Path.Combine(desktop, $"links_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllLines(path, _links);
                Log($"Сохранено {_links.Count} ссылок в файл: {path}");
            }
            catch (Exception ex) { Log($"ОШИБКА: {ex.Message}"); }
        }

        private void UpdateStepsList()
        {
            lstSteps.Items.Clear();
            for (int i = 0; i < _steps.Count; i++)
                lstSteps.Items.Add($"{i + 1}. {_steps[i]} (задержка: {_steps[i].DelayMs / 1000} сек)");
        }

        private void Log(string message)
        {
            if (rtbLog.InvokeRequired) { rtbLog.Invoke(new Action<string>(Log), message); return; }

            string time = $"[{DateTime.Now:HH:mm:ss}] ";
            bool isError = message.Contains("ОШИБКА", StringComparison.OrdinalIgnoreCase) || message.Contains("!!!");

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionColor = Color.Gray;
            rtbLog.AppendText(time);

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionColor = isError ? Color.Crimson : Color.FromArgb(40, 40, 40);
            rtbLog.AppendText(message + Environment.NewLine);

            rtbLog.ScrollToCaret();

            _consoleForm?.Log(message);
        }
    }
}