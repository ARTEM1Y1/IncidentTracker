using IncidentTracker.Data;
using IncidentTracker.Models;
using IncidentTracker.Services;

namespace IncidentTracker.Forms
{
    public class ReportForm : Form
    {
        private readonly DatabaseManager _db;

        public ReportForm(DatabaseManager db)
        {
            _db = db;
            BuildUI();
            LoadReport();
        }

        private void BuildUI()
        {
            Text = "Сводный отчёт";
            Size = new Size(700, 580);
            StartPosition = FormStartPosition.CenterParent;

            var tabs = new TabControl { Dock = DockStyle.Fill };

            var tabStatus = new TabPage("По статусам");
            var gridStatus = BuildGrid();
            gridStatus.Name = "gridStatus";
            tabStatus.Controls.Add(gridStatus);
            gridStatus.Columns.Add("Status", "Статус");
            gridStatus.Columns.Add("Count", "Кол-во");
            gridStatus.Columns[0].FillWeight = 200;
            gridStatus.Columns[1].FillWeight = 80;

            var tabCat = new TabPage("По категориям");
            var gridCat = BuildGrid();
            gridCat.Name = "gridCat";
            tabCat.Controls.Add(gridCat);
            gridCat.Columns.Add("Cat", "Категория");
            gridCat.Columns.Add("Count", "Кол-во");

            var tabOver = new TabPage("Просроченные");
            var gridOver = BuildGrid();
            gridOver.Name = "gridOver";
            tabOver.Controls.Add(gridOver);
            gridOver.Columns.Add("Number",   "Номер");
            gridOver.Columns.Add("Author",   "Автор");
            gridOver.Columns.Add("Priority", "Приоритет");
            gridOver.Columns.Add("Deadline", "Срок решения");
            gridOver.Columns.Add("Status",   "Статус");

            tabs.TabPages.Add(tabStatus);
            tabs.TabPages.Add(tabCat);
            tabs.TabPages.Add(tabOver);

            var btnPanel = new Panel { Height = 42, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(240, 245, 255) };
            var btnExport = new Button { Text = "Экспорт в CSV", Left = 8, Top = 6, Width = 130, Height = 30, BackColor = Color.FromArgb(0,150,80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += ExportCsv;
            var btnClose = new Button { Text = "Закрыть", Left = 146, Top = 6, Width = 90, Height = 30, BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnClose.FlatAppearance.BorderSize = 0;
            btnPanel.Controls.AddRange(new Control[] { btnExport, btnClose });

            Controls.Add(tabs);
            Controls.Add(btnPanel);

            _gridStatus = gridStatus;
            _gridCat    = gridCat;
            _gridOver   = gridOver;
            _tabs       = tabs;
        }

        private DataGridView _gridStatus = null!, _gridCat = null!, _gridOver = null!;
        private TabControl _tabs = null!;

        private static DataGridView BuildGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f), ColumnHeadersHeight = 30
            };
        }

        private void LoadReport()
        {
            var statusData = _db.GetStatusReport();
            foreach (var kv in statusData)
                _gridStatus.Rows.Add(kv.Key, kv.Value);

            var catData = _db.GetCategoryReport();
            foreach (var kv in catData)
                _gridCat.Rows.Add(kv.Key, kv.Value);

            var overdue = _db.GetOverdueIncidents();
            foreach (var inc in overdue)
            {
                var row = _gridOver.Rows[_gridOver.Rows.Add()];
                row.Cells["Number"].Value   = inc.Number;
                row.Cells["Author"].Value   = inc.Author;
                row.Cells["Priority"].Value = StatusHelper.ToRussian(inc.Priority);
                row.Cells["Deadline"].Value = inc.ResolutionDeadline?.ToString("dd.MM.yyyy HH:mm");
                row.Cells["Status"].Value   = StatusHelper.ToRussian(inc.Status);
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
            }
        }

        private void ExportCsv(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog { Filter = "CSV файлы (*.csv)|*.csv", FileName = $"report_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            var grid = (_tabs.SelectedTab?.Controls[0] as DataGridView) ?? _gridStatus;
            var lines = new List<string>();
            lines.Add(string.Join(";", grid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText)));
            foreach (DataGridViewRow row in grid.Rows)
                lines.Add(string.Join(";", row.Cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? "")));
            File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
            MessageBox.Show("Экспорт завершён!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
