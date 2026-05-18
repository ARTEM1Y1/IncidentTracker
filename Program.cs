using IncidentTracker.Data;
using IncidentTracker.Forms;

namespace IncidentTracker
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var db = new DatabaseManager();
            Application.Run(new MainForm(db));
        }
    }
}
