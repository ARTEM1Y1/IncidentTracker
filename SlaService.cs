using IncidentTracker.Models;

namespace IncidentTracker.Services
{
    public static class SlaService
    {
        // SLA in hours by priority
        private static readonly Dictionary<Priority, (int Reaction, int Resolution)> SlaHours = new()
        {
            { Priority.Critical, (1,  4)  },
            { Priority.High,     (4,  24) },
            { Priority.Medium,   (8,  72) },
            { Priority.Low,      (24, 168) }
        };

        public static (DateTime Reaction, DateTime Resolution) Calculate(Priority priority, DateTime createdAt)
        {
            var (r, res) = SlaHours[priority];
            return (createdAt.AddHours(r), createdAt.AddHours(res));
        }

        public static string GetSlaDescription(Priority priority)
        {
            var (r, res) = SlaHours[priority];
            return $"Реакция: {r} ч., Решение: {res} ч.";
        }

        public static Color GetStatusColor(Incident inc)
        {
            if (inc.Status == IncidentStatus.Closed || inc.Status == IncidentStatus.Resolved || inc.Status == IncidentStatus.Rejected)
                return Color.FromArgb(220, 255, 220);
            if (inc.IsOverdue)
                return Color.FromArgb(255, 200, 200);
            if (inc.ResolutionDeadline.HasValue && inc.ResolutionDeadline.Value < DateTime.Now.AddHours(4))
                return Color.FromArgb(255, 240, 180);
            return Color.White;
        }
    }
}
