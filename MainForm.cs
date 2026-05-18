using IncidentTracker.Data;
using IncidentTracker.Models;
using IncidentTracker.Services;

namespace IncidentTracker.Forms
{
    public class MainForm : Form
    {
        private readonly DatabaseManager _db;
        private DataGridView _grid = null!;
        private TextBox _searchBox = null!;
        private ComboBox _statusFilter = null!;
        private Label _countLabel = null!;
        private System.Windows.Forms.Timer _slaTimer = null!;

        public MainForm(DatabaseManager db)
        {
            _db = db;
            BuildUI();
            RefreshGrid();
        }

        private void BuildUI()
        {
            Text = "Система учёта инцидентов техподдержки";
            Size = new Size(1200, 700);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 500);

            var menu = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            fileMenu.DropDownItems.Add("Новый инцидент", null, (s, e) => OpenNewIncident());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Журнал приложения", null, (s, e) => new LogForm().ShowDialog(this));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Выход", null, (s, e) => Application.Exit());
            var reportMenu = new ToolStripMenuItem("Отчёты");
            reportMenu.DropDownItems.Add("Сводный отчёт", null, (s, e) => new ReportForm(_db).ShowDialog(this));
            reportMenu.DropDownItems.Add("Просроченные инциденты", null, (s, e) => ShowOverdue());
            menu.Items.Add(fileMenu);
            menu.Items.Add(reportMenu);
            MainMenuStrip = menu;

            // Toolbar
            var toolbar = new Panel { Height = 46, Dock = DockStyle.Top, BackColor = Color.FromArgb(240, 245, 255) };

            var btnNew = new Button
            {
                Text = "＋ Новый инцидент",
                Width = 150, Height = 32, Left = 8, Top = 7,
                BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Click += (s, e) => OpenNewIncident();

            var btnRefresh = new Button
            {
                Text = "↺ Обновить",
                Width = 110, Height = 32, Left = 170, Top = 7,
                BackColor = Color.FromArgb(100, 160, 240), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshGrid();

            var lblSearch = new Label { Text = "Поиск:", Left = 300, Top = 14, AutoSize = true };
            _searchBox = new TextBox { Left = 350, Top = 10, Width = 220, Height = 28 };
            _searchBox.TextChanged += (s, e) => RefreshGrid();

            var lblStatus = new Label { Text = "Статус:", Left = 590, Top = 14, AutoSize = true };
            _statusFilter = new ComboBox { Left = 640, Top = 10, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            _statusFilter.Items.Add("Все статусы");
            foreach (IncidentStatus s in Enum.GetValues(typeof(IncidentStatus)))
                _statusFilter.Items.Add(StatusHelper.ToRussian(s));
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += (s, e) => RefreshGrid();

            _countLabel = new Label { Left = 820, Top = 14, AutoSize = true, Font = new Font("Segoe UI", 9f) };

            toolbar.Controls.AddRange(new Control[] { btnNew, btnRefresh, lblSearch, _searchBox, lblStatus, _statusFilter, _countLabel });

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 26 }
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 100, 180);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _grid.EnableHeadersVisualStyles = false;
            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenSelectedIncident(); };
            _grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && _grid.CurrentRow != null) OpenSelectedIncident(); };

            // Columns
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Number",     HeaderText = "Номер",        FillWeight = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Author",     HeaderText = "Автор",        FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Department", HeaderText = "Подразделение",FillWeight = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category",   HeaderText = "Категория",    FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Priority",   HeaderText = "Приоритет",    FillWeight = 80  });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",     HeaderText = "Статус",       FillWeight = 110 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt",  HeaderText = "Создан",       FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Deadline",   HeaderText = "Срок решения", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssignedTo", HeaderText = "Исполнитель",  FillWeight = 100 });

            var statusBar = new Panel { Height = 24, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(230, 235, 245) };
            var statusLabel = new Label { Text = "  Система учёта инцидентов v1.0", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8f) };
            statusBar.Controls.Add(statusLabel);

            Controls.Add(_grid);
            Controls.Add(toolbar);
            Controls.Add(menu);
            Controls.Add(statusBar);

            _slaTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            _slaTimer.Tick += (s, e) => RefreshGrid();
            _slaTimer.Start();

            Logger.Log("Приложение запущено");
        }

        private void RefreshGrid()
        {
            IncidentStatus? statusFilter = null;
            if (_statusFilter.SelectedIndex > 0)
                statusFilter = (IncidentStatus)(_statusFilter.SelectedIndex - 1);

            var incidents = _db.GetAllIncidents(_searchBox.Text, statusFilter);
            _grid.Rows.Clear();
            foreach (var inc in incidents)
            {
                var row = _grid.Rows[_grid.Rows.Add()];
                row.Cells["Number"].Value     = inc.Number;
                row.Cells["Author"].Value     = inc.Author;
                row.Cells["Department"].Value = inc.Department;
                row.Cells["Category"].Value   = inc.Category;
                row.Cells["Priority"].Value   = StatusHelper.ToRussian(inc.Priority);
                row.Cells["Status"].Value     = StatusHelper.ToRussian(inc.Status);
                row.Cells["CreatedAt"].Value  = inc.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                row.Cells["Deadline"].Value   = inc.ResolutionDeadline?.ToString("dd.MM.yyyy HH:mm") ?? "—";
                row.Cells["AssignedTo"].Value = inc.AssignedTo ?? "—";
                row.Tag = inc.Id;
                row.DefaultCellStyle.BackColor = SlaService.GetStatusColor(inc);
            }
            _countLabel.Text = $"Всего: {incidents.Count}  |  Просроченных: {incidents.Count(i => i.IsOverdue)}";
        }

        private void OpenNewIncident()
        {
            var form = new IncidentForm(_db, null);
            if (form.ShowDialog(this) == DialogResult.OK) RefreshGrid();
        }

        private void OpenSelectedIncident()
        {
            if (_grid.CurrentRow?.Tag is int id)
            {
                var inc = _db.GetIncident(id);
                if (inc != null)
                {
                    var form = new IncidentForm(_db, inc);
                    if (form.ShowDialog(this) == DialogResult.OK) RefreshGrid();
                }
            }
        }

        private void ShowOverdue()
        {
            var overdue = _db.GetOverdueIncidents();
            MessageBox.Show($"Просроченных инцидентов: {overdue.Count}\n\n" +
                string.Join("\n", overdue.Select(i => $"• {i.Number} – {i.Author} [{StatusHelper.ToRussian(i.Status)}]")),
                "Просроченные инциденты", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
