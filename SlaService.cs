using IncidentTracker.Models;

namespace IncidentTracker.Services
{
    public static class SlaService
    {
        private static readonly Dictionary<Priority, int> ReactionHours = new()
        {
            { Priority.Critical, 1  },
            { Priority.High,     4  },
            { Priority.Medium,   8  },
            { Priority.Low,      24 }
        };

        private static readonly Dictionary<Priority, int> ResolutionHours = new()
        {
            { Priority.Critical, 4   },
            { Priority.High,     24  },
            { Priority.Medium,   72  },
            { Priority.Low,      168 }
        };

        public static (DateTime Reaction, DateTime Resolution) Calculate(Priority priority, DateTime createdAt)
        {
            return (
                createdAt.AddHours(ReactionHours[priority]),
                createdAt.AddHours(ResolutionHours[priority])
            );
        }

        public static string GetSlaDescription(Priority priority) =>
            $"Реакция: {ReactionHours[priority]} ч.  |  Решение: {ResolutionHours[priority]} ч.";
        public static string GetSlaCategoryLabel(Priority priority) => priority switch
        {
            Priority.Critical => "🔴 SLA: Критический (реакция 1 ч.)",
            Priority.High     => "🟠 SLA: Высокий (реакция 4 ч.)",
            Priority.Medium   => "🟡 SLA: Средний (реакция 8 ч.)",
            Priority.Low      => "🟢 SLA: Низкий (реакция 24 ч.)",
            _                 => ""
        };

        public static Color GetStatusColor(Incident inc)
        {
            if (inc.Status is IncidentStatus.Closed or IncidentStatus.Resolved or IncidentStatus.Rejected)
                return Color.FromArgb(220, 255, 220);
            if (inc.IsOverdue)
                return Color.FromArgb(255, 200, 200);
            if (inc.ResolutionDeadline.HasValue && inc.ResolutionDeadline.Value < DateTime.Now.AddHours(4))
                return Color.FromArgb(255, 240, 180);
            return Color.White;
        }
    }
}
