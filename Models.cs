namespace IncidentTracker.Models
{
    public enum IncidentStatus
    {
        New = 0,
        InProgress = 1,
        WaitingInfo = 2,
        Resolved = 3,
        Closed = 4,
        Rejected = 5
    }

    public enum Priority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    public class Incident
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";
        public string Author { get; set; } = "";
        public string Department { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public Priority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReactionDeadline { get; set; }
        public DateTime? ResolutionDeadline { get; set; }
        public string? AssignedTo { get; set; }
        public IncidentStatus Status { get; set; }

        public bool IsOverdue => ResolutionDeadline.HasValue && ResolutionDeadline.Value < DateTime.Now
            && Status != IncidentStatus.Resolved && Status != IncidentStatus.Closed && Status != IncidentStatus.Rejected;

        public bool IsDescriptionLocked => Status != IncidentStatus.New;

        public bool CanTransitionToClosed => Status == IncidentStatus.Resolved;
    }

    public class HistoryEntry
    {
        public int Id { get; set; }
        public int IncidentId { get; set; }
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = "";
        public string Action { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }
        public int IncidentId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Author { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public class Attachment
    {
        public int Id { get; set; }
        public int IncidentId { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public static class StatusHelper
    {
        public static string ToRussian(IncidentStatus status) => status switch
        {
            IncidentStatus.New         => "Новый",
            IncidentStatus.InProgress  => "В работе",
            IncidentStatus.WaitingInfo => "Ожидает информации",
            IncidentStatus.Resolved    => "Решён",
            IncidentStatus.Closed      => "Закрыт",
            IncidentStatus.Rejected    => "Отклонён",
            _                          => status.ToString()
        };

        public static string ToRussian(Priority priority) => priority switch
        {
            Priority.Low      => "Низкий",
            Priority.Medium   => "Средний",
            Priority.High     => "Высокий",
            Priority.Critical => "Критический",
            _                 => priority.ToString()
        };
    }
}
