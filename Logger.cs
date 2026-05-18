namespace IncidentTracker.Services
{
    public static class Logger
    {
        private static readonly string LogPath = "app.log";

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { /* silent */ }
        }

        public static string[] GetRecentLogs(int lines = 200)
        {
            try
            {
                if (!File.Exists(LogPath)) return Array.Empty<string>();
                var all = File.ReadAllLines(LogPath);
                return all.TakeLast(lines).ToArray();
            }
            catch { return Array.Empty<string>(); }
        }
    }
}
