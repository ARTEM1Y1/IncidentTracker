using IncidentTracker.Data;
using IncidentTracker.Models;
using IncidentTracker.Services;

namespace IncidentTracker.Forms
{
    public class IncidentForm : Form
    {
        private readonly DatabaseManager _db;
        private readonly Incident? _existing;
        private readonly string _currentUser = Environment.UserName;

        private TextBox _txtNumber = null!, _txtAuthor = null!, _txtDepartment = null!;
        private ComboBox _cmbCategory = null!, _cmbPriority = null!, _cmbStatus = null!, _cmbAssigned = null!;
        private RichTextBox _txtDescription = null!;
        private DateTimePicker _dtpReaction = null!, _dtpResolution = null!;
        private CheckBox _chkReaction = null!, _chkResolution = null!;
        private TabControl _tabs = null!;
        private ListBox _lstComments = null!, _lstAttachments = null!, _lstHistory = null!;
        private TextBox _txtNewComment = null!;
        private Label _lblSla = null!;
        private Label _lblDescLock = null!;

        private static readonly string[] Categories = { "Сеть", "ПК/Ноутбук", "Программное обеспечение", "Принтер", "Телефония", "Сервер", "Почта", "Безопасность", "Другое" };
        private static readonly string[] Assignees = { "Иванов И.И.", "Петров П.П.", "Сидоров С.С.", "Козлов К.К.", "Новиков Н.Н." };

        public IncidentForm(DatabaseManager db, Incident? existing)
        {
            _db = db;
            _existing = existing;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            Text = _existing == null ? "Новый инцидент" : $"Инцидент {_existing.Number}";
            Size = new Size(820, 680);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(700, 580);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10),
                RowCount = 2
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 310));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var leftGroup = new GroupBox { Text = "Данные инцидента", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var leftPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(6), AutoScroll = true };
            leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _txtNumber     = AddField(leftPanel, "Номер:", new TextBox { ReadOnly = true, BackColor = Color.FromArgb(240, 240, 240) });
            _txtAuthor     = AddField(leftPanel, "Автор:", new TextBox());
            _txtDepartment = AddField(leftPanel, "Подразделение:", new TextBox());
            _cmbCategory   = AddField(leftPanel, "Категория:", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            _cmbPriority   = AddField(leftPanel, "Приоритет:", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            _cmbStatus     = AddField(leftPanel, "Статус:", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            _cmbAssigned   = AddField(leftPanel, "Исполнитель:", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });

            _cmbCategory.Items.AddRange(Categories);
            _cmbCategory.SelectedIndex = 0;

            foreach (Priority p in Enum.GetValues(typeof(Priority)))
                _cmbPriority.Items.Add(StatusHelper.ToRussian(p));
            _cmbPriority.SelectedIndex = 1;
            _cmbPriority.SelectedIndexChanged += (s, e) => UpdateSla();

            foreach (IncidentStatus st in Enum.GetValues(typeof(IncidentStatus)))
                _cmbStatus.Items.Add(StatusHelper.ToRussian(st));
            _cmbStatus.SelectedIndex = 0;

            _cmbStatus.SelectedIndexChanged += (s, e) => ApplyBusinessRules();

            _cmbAssigned.Items.Add("Не назначен");
            _cmbAssigned.Items.AddRange(Assignees);
            _cmbAssigned.SelectedIndex = 0;

            leftGroup.Controls.Add(leftPanel);

            var rightGroup = new GroupBox { Text = "Описание и SLA", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };

            _lblDescLock = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                AutoSize = false,
                Text = "⚠  Описание заблокировано — добавляйте комментарии на вкладке ниже",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 80, 0),
                BackColor = Color.FromArgb(255, 245, 220),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false,
                Padding = new Padding(4, 0, 0, 0)
            };

            _txtDescription = new RichTextBox
            {
                Dock = DockStyle.Top, Height = 100,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblSla = new Label
            {
                AutoSize = false, Height = 22, Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(0, 80, 180)
            };

            var slaPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 60, ColumnCount = 2 };
            slaPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            slaPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _chkReaction   = new CheckBox { Text = "Срок реакции:",  Checked = true, AutoSize = true };
            _dtpReaction   = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy HH:mm" };
            _chkResolution = new CheckBox { Text = "Срок решения:", Checked = true, AutoSize = true };
            _dtpResolution = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy HH:mm" };
            _chkReaction.CheckedChanged   += (s, e) => _dtpReaction.Enabled   = _chkReaction.Checked;
            _chkResolution.CheckedChanged += (s, e) => _dtpResolution.Enabled = _chkResolution.Checked;

            slaPanel.Controls.Add(_chkReaction,   0, 0); slaPanel.Controls.Add(_dtpReaction,   1, 0);
            slaPanel.Controls.Add(_chkResolution, 0, 1); slaPanel.Controls.Add(_dtpResolution, 1, 1);

            rightPanel.Controls.Add(slaPanel);
            rightPanel.Controls.Add(_lblSla);
            rightPanel.Controls.Add(_lblDescLock);
            rightPanel.Controls.Add(_txtDescription);
            rightGroup.Controls.Add(rightPanel);

            mainPanel.Controls.Add(leftGroup,  0, 0);
            mainPanel.Controls.Add(rightGroup, 1, 0);

            _tabs = new TabControl { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(_tabs, 0, 1);
            mainPanel.SetColumnSpan(_tabs, 2);

            var tabComments = new TabPage("Комментарии");
            var commentPanel = new Panel { Dock = DockStyle.Fill };
            _lstComments = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), IntegralHeight = false };
            var commentBottom = new Panel { Dock = DockStyle.Bottom, Height = 36 };
            _txtNewComment = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Введите комментарий..." };
            var btnAddComment = new Button
            {
                Text = "Добавить", Dock = DockStyle.Right, Width = 90,
                BackColor = Color.FromArgb(0, 150, 136), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnAddComment.FlatAppearance.BorderSize = 0;
            btnAddComment.Click += AddComment;
            commentBottom.Controls.Add(_txtNewComment);
            commentBottom.Controls.Add(btnAddComment);
            commentPanel.Controls.Add(_lstComments);
            commentPanel.Controls.Add(commentBottom);
            tabComments.Controls.Add(commentPanel);

            var tabAttach = new TabPage("Файлы");
            var attachPanel = new Panel { Dock = DockStyle.Fill };
            _lstAttachments = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), IntegralHeight = false };
            var attachBottom = new Panel { Dock = DockStyle.Bottom, Height = 36 };
            var btnAddFile = new Button { Text = "Прикрепить файл", Left = 4,   Top = 4, Width = 140, Height = 28, BackColor = Color.FromArgb(0, 150, 136), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnOpenFile = new Button { Text = "Открыть",        Left = 152, Top = 4, Width = 90,  Height = 28, BackColor = Color.FromArgb(100, 100, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnDelFile  = new Button { Text = "Удалить",        Left = 250, Top = 4, Width = 90,  Height = 28, BackColor = Color.FromArgb(200, 80, 80),   ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAddFile.FlatAppearance.BorderSize = btnOpenFile.FlatAppearance.BorderSize = btnDelFile.FlatAppearance.BorderSize = 0;
            btnAddFile.Click  += AddAttachment;
            btnOpenFile.Click += OpenAttachment;
            btnDelFile.Click  += DeleteAttachment;
            attachBottom.Controls.AddRange(new Control[] { btnAddFile, btnOpenFile, btnDelFile });
            attachPanel.Controls.Add(_lstAttachments);
            attachPanel.Controls.Add(attachBottom);
            tabAttach.Controls.Add(attachPanel);

            var tabHistory = new TabPage("История");
            _lstHistory = new ListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 8.5f), IntegralHeight = false };
            tabHistory.Controls.Add(_lstHistory);

            _tabs.TabPages.Add(tabComments);
            _tabs.TabPages.Add(tabAttach);
            _tabs.TabPages.Add(tabHistory);

            var btnPanel = new Panel { Height = 46, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(240, 245, 255) };
            var btnSave = new Button
            {
                Text = "Сохранить", DialogResult = DialogResult.OK,
                Width = 120, Height = 32, Left = 8, Top = 7,
                BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += SaveIncident;
            var btnCancel = new Button
            {
                Text = "Отмена", DialogResult = DialogResult.Cancel,
                Width = 100, Height = 32, Left = 136, Top = 7,
                BackColor = Color.FromArgb(160, 160, 160), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnPanel.Controls.AddRange(new Control[] { btnSave, btnCancel });

            Controls.Add(mainPanel);
            Controls.Add(btnPanel);

            UpdateSla();
        }

        private T AddField<T>(TableLayoutPanel panel, string label, T control) where T : Control
        {
            panel.Controls.Add(new Label
            {
                Text = label, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9f)
            });
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 2, 0, 2);
            panel.Controls.Add(control);
            return control;
        }

        private void LoadData()
        {
            if (_existing == null)
            {
                _txtNumber.Text = _db.GenerateNumber();
                _txtAuthor.Text = _currentUser;
                UpdateSla();
                return;
            }

            _txtNumber.Text     = _existing.Number;
            _txtAuthor.Text     = _existing.Author;
            _txtDepartment.Text = _existing.Department;
            _cmbCategory.Text   = _existing.Category;
            _cmbPriority.SelectedIndex = (int)_existing.Priority;
            _cmbStatus.SelectedIndex   = (int)_existing.Status;
            _cmbAssigned.Text = _existing.AssignedTo ?? "Не назначен";
            _txtDescription.Text = _existing.Description;

            if (_existing.ReactionDeadline.HasValue)   { _chkReaction.Checked = true;   _dtpReaction.Value   = _existing.ReactionDeadline.Value; }
            else                                        { _chkReaction.Checked = false;  _dtpReaction.Enabled = false; }
            if (_existing.ResolutionDeadline.HasValue) { _chkResolution.Checked = true;  _dtpResolution.Value = _existing.ResolutionDeadline.Value; }
            else                                        { _chkResolution.Checked = false; _dtpResolution.Enabled = false; }

            LoadComments();
            LoadAttachments();
            LoadHistory();
            UpdateSla();
            ApplyBusinessRules();
        }

        private void UpdateSla()
        {
            var priority = (Priority)Math.Max(0, _cmbPriority.SelectedIndex);
            _lblSla.Text = SlaService.GetSlaCategoryLabel(priority)
                         + "     " + SlaService.GetSlaDescription(priority);

            if (_existing == null)
            {
                var (r, res) = SlaService.Calculate(priority, DateTime.Now);
                if (_chkReaction.Checked)   _dtpReaction.Value   = r;
                if (_chkResolution.Checked) _dtpResolution.Value = res;
            }
        }

        private void ApplyBusinessRules()
        {
            bool descLocked = _existing != null && _existing.IsDescriptionLocked;
            _txtDescription.ReadOnly  = descLocked;
            _txtDescription.BackColor = descLocked ? Color.FromArgb(245, 245, 245) : Color.White;
            _lblDescLock.Visible      = descLocked;

            var selectedText = _cmbStatus.SelectedItem?.ToString() ?? "";
            _cmbStatus.SelectedIndexChanged -= (s, e) => ApplyBusinessRules();

            _cmbStatus.Items.Clear();
            foreach (IncidentStatus st in Enum.GetValues(typeof(IncidentStatus)))
            {
                if (st == IncidentStatus.Closed && _existing != null && !_existing.CanTransitionToClosed)
                    continue;
                _cmbStatus.Items.Add(StatusHelper.ToRussian(st));
            }

            int idx = _cmbStatus.Items.IndexOf(selectedText);
            _cmbStatus.SelectedIndex = idx >= 0 ? idx : 0;

            _cmbStatus.SelectedIndexChanged += (s, e) => ApplyBusinessRules(); 
        }

        private void SaveIncident(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtAuthor.Text) ||
                string.IsNullOrWhiteSpace(_txtDepartment.Text) ||
                string.IsNullOrWhiteSpace(_txtDescription.Text))
            {
                MessageBox.Show("Заполните обязательные поля: Автор, Подразделение, Описание.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            var chosenText = _cmbStatus.SelectedItem?.ToString() ?? "";
            IncidentStatus newStatus = Enum.GetValues<IncidentStatus>()
                .FirstOrDefault(s => StatusHelper.ToRussian(s) == chosenText);

            if (newStatus == IncidentStatus.Closed)
            {
                if (_existing == null || !_existing.CanTransitionToClosed)
                {
                    MessageBox.Show(
                        "Статус «Закрыт» можно выставить только после статуса «Решён».",
                        "Нарушение бизнес-правила", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                if (string.IsNullOrWhiteSpace(_txtNewComment.Text))
                {
                    MessageBox.Show(
                        "Для перевода инцидента в статус «Закрыт» необходимо добавить комментарий с описанием решения.\n\n" +
                        "Заполните поле комментария и нажмите «Сохранить» снова.",
                        "Требуется комментарий", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _tabs.SelectedIndex = 0; 
                    _txtNewComment.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }
            }

            if (_existing == null)
            {
                var inc = new Incident
                {
                    Number             = _txtNumber.Text,
                    Author             = _txtAuthor.Text,
                    Department         = _txtDepartment.Text,
                    Category           = _cmbCategory.Text,
                    Description        = _txtDescription.Text,
                    Priority           = (Priority)_cmbPriority.SelectedIndex,
                    CreatedAt          = DateTime.Now,
                    ReactionDeadline   = _chkReaction.Checked   ? _dtpReaction.Value   : null,
                    ResolutionDeadline = _chkResolution.Checked ? _dtpResolution.Value : null,
                    AssignedTo         = _cmbAssigned.SelectedIndex > 0 ? _cmbAssigned.Text : null,
                    Status             = newStatus
                };
                var id = _db.CreateIncident(inc);
                _db.AddHistory(new HistoryEntry { IncidentId = id, Timestamp = DateTime.Now, User = _currentUser, Action = "Создан инцидент" });
                Logger.Log($"Создан инцидент {inc.Number}");
            }
            else
            {
                var oldStatus = _existing.Status;

                if (!_existing.IsDescriptionLocked)
                    _existing.Description = _txtDescription.Text;

                _existing.Author             = _txtAuthor.Text;
                _existing.Department         = _txtDepartment.Text;
                _existing.Category           = _cmbCategory.Text;
                _existing.Priority           = (Priority)_cmbPriority.SelectedIndex;
                _existing.ReactionDeadline   = _chkReaction.Checked   ? _dtpReaction.Value   : null;
                _existing.ResolutionDeadline = _chkResolution.Checked ? _dtpResolution.Value : null;
                _existing.AssignedTo         = _cmbAssigned.SelectedIndex > 0 ? _cmbAssigned.Text : null;
                _existing.Status             = newStatus;

                _db.UpdateIncident(_existing);

                if (oldStatus != newStatus)
                {
                    _db.AddHistory(new HistoryEntry
                    {
                        IncidentId = _existing.Id, Timestamp = DateTime.Now, User = _currentUser,
                        Action     = "Изменён статус",
                        OldValue   = StatusHelper.ToRussian(oldStatus),
                        NewValue   = StatusHelper.ToRussian(newStatus)
                    });
                }

                if (!string.IsNullOrWhiteSpace(_txtNewComment.Text))
                {
                    _db.AddComment(new Comment
                    {
                        IncidentId = _existing.Id, Timestamp = DateTime.Now,
                        Author     = _currentUser,
                        Text       = _txtNewComment.Text.Trim()
                    });
                    _db.AddHistory(new HistoryEntry
                    {
                        IncidentId = _existing.Id, Timestamp = DateTime.Now,
                        User       = _currentUser, Action = "Добавлен комментарий при закрытии"
                    });
                    _txtNewComment.Clear();
                }

                Logger.Log($"Обновлён инцидент {_existing.Number}  ({StatusHelper.ToRussian(oldStatus)} → {StatusHelper.ToRussian(newStatus)})");
            }
        }


        private void AddComment(object? sender, EventArgs e)
        {
            if (_existing == null)
            {
                MessageBox.Show("Сначала сохраните инцидент.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtNewComment.Text)) return;

            _db.AddComment(new Comment
            {
                IncidentId = _existing.Id, Timestamp = DateTime.Now,
                Author = _currentUser, Text = _txtNewComment.Text.Trim()
            });
            _db.AddHistory(new HistoryEntry
            {
                IncidentId = _existing.Id, Timestamp = DateTime.Now,
                User = _currentUser, Action = "Добавлен комментарий"
            });
            _txtNewComment.Clear();
            LoadComments();
        }


        private void AddAttachment(object? sender, EventArgs e)
        {
            if (_existing == null) { MessageBox.Show("Сначала сохраните инцидент.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using var ofd = new OpenFileDialog { Title = "Выберите файл", Multiselect = false };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var src = ofd.FileName;
            var dir = Path.Combine("Attachments", _existing.Id.ToString());
            Directory.CreateDirectory(dir);
            var dest = Path.Combine(dir, Path.GetFileName(src));
            File.Copy(src, dest, true);
            _db.AddAttachment(new Attachment
            {
                IncidentId = _existing.Id, FileName = Path.GetFileName(src),
                FilePath = dest, FileSize = new FileInfo(dest).Length, UploadedAt = DateTime.Now
            });
            _db.AddHistory(new HistoryEntry
            {
                IncidentId = _existing.Id, Timestamp = DateTime.Now,
                User = _currentUser, Action = "Прикреплён файл", NewValue = Path.GetFileName(src)
            });
            LoadAttachments();
        }

        private void OpenAttachment(object? sender, EventArgs e)
        {
            if (_lstAttachments.SelectedItem is Attachment a && File.Exists(a.FilePath))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(a.FilePath) { UseShellExecute = true });
        }

        private void DeleteAttachment(object? sender, EventArgs e)
        {
            if (_lstAttachments.SelectedItem is not Attachment a) return;
            if (MessageBox.Show($"Удалить файл «{a.FileName}»?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { File.Delete(a.FilePath); } catch { }
            _db.DeleteAttachment(a.Id);
            LoadAttachments();
        }

        private void LoadComments()
        {
            if (_existing == null) return;
            _lstComments.Items.Clear();
            foreach (var c in _db.GetComments(_existing.Id))
                _lstComments.Items.Add($"[{c.Timestamp:dd.MM.yyyy HH:mm}] {c.Author}: {c.Text}");
        }

        private void LoadAttachments()
        {
            if (_existing == null) return;
            _lstAttachments.DataSource = null;
            _lstAttachments.DataSource = _db.GetAttachments(_existing.Id);
            _lstAttachments.DisplayMember = "FileName";
        }

        private void LoadHistory()
        {
            if (_existing == null) return;
            _lstHistory.Items.Clear();
            foreach (var h in _db.GetHistory(_existing.Id))
            {
                var line = $"[{h.Timestamp:dd.MM.yyyy HH:mm}] {h.User} — {h.Action}";
                if (h.OldValue != null || h.NewValue != null)
                    line += $"  ({h.OldValue} → {h.NewValue})";
                _lstHistory.Items.Add(line);
            }
        }
    }
}
