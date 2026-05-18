using IncidentTracker.Services;

namespace IncidentTracker.Forms
{
    public class LogForm : Form
    {
        public LogForm()
        {
            Text = "Журнал приложения";
            Size = new Size(720, 500);
            StartPosition = FormStartPosition.CenterParent;

            var box = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                Font = new Font("Consolas", 9f), BackColor = Color.FromArgb(20, 20, 30), ForeColor = Color.LightGreen,
                BorderStyle = BorderStyle.None
            };
            var btnPanel = new Panel { Height = 40, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(240, 245, 255) };
            var btnClose = new Button { Text = "Закрыть", Left = 8, Top = 6, Width = 90, Height = 28, BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnClose.FlatAppearance.BorderSize = 0;
            var btnRefresh = new Button { Text = "Обновить", Left = 106, Top = 6, Width = 90, Height = 28, BackColor = Color.FromArgb(0,120,212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => box.Lines = Logger.GetRecentLogs();
            btnPanel.Controls.AddRange(new Control[] { btnClose, btnRefresh });
            Controls.Add(box);
            Controls.Add(btnPanel);
            box.Lines = Logger.GetRecentLogs();
        }
    }
}
