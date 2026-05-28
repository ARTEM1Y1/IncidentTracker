using Microsoft.Data.Sqlite;
using IncidentTracker.Models;

namespace IncidentTracker.Data
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string dbPath = "incidents.db")
        {
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Incidents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Number TEXT NOT NULL UNIQUE,
                    Author TEXT NOT NULL,
                    Department TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Priority INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    ReactionDeadline TEXT,
                    ResolutionDeadline TEXT,
                    AssignedTo TEXT,
                    Status INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS History (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IncidentId INTEGER NOT NULL,
                    Timestamp TEXT NOT NULL,
                    User TEXT NOT NULL,
                    Action TEXT NOT NULL,
                    OldValue TEXT,
                    NewValue TEXT,
                    FOREIGN KEY(IncidentId) REFERENCES Incidents(Id)
                );

                CREATE TABLE IF NOT EXISTS Comments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IncidentId INTEGER NOT NULL,
                    Timestamp TEXT NOT NULL,
                    Author TEXT NOT NULL,
                    Text TEXT NOT NULL,
                    FOREIGN KEY(IncidentId) REFERENCES Incidents(Id)
                );

                CREATE TABLE IF NOT EXISTS Attachments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IncidentId INTEGER NOT NULL,
                    FileName TEXT NOT NULL,
                    FilePath TEXT NOT NULL,
                    FileSize INTEGER NOT NULL,
                    UploadedAt TEXT NOT NULL,
                    FOREIGN KEY(IncidentId) REFERENCES Incidents(Id)
                );
            ";
            cmd.ExecuteNonQuery();
        }

        //incidents 

        public List<Incident> GetAllIncidents(string? search = null, IncidentStatus? status = null, string? assignedTo = null)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            var where = new List<string>();
            if (!string.IsNullOrWhiteSpace(search))
            {
                where.Add("(Number LIKE $s OR Author LIKE $s OR Department LIKE $s OR Description LIKE $s OR Category LIKE $s)");
                cmd.Parameters.AddWithValue("$s", $"%{search}%");
            }
            if (status.HasValue)
            {
                where.Add("Status = $st");
                cmd.Parameters.AddWithValue("$st", (int)status.Value);
            }
            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                where.Add("AssignedTo = $a");
                cmd.Parameters.AddWithValue("$a", assignedTo);
            }
            cmd.CommandText = "SELECT * FROM Incidents" + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") + " ORDER BY CreatedAt DESC";
            var list = new List<Incident>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(ReadIncident(reader));
            return list;
        }

        public Incident? GetIncident(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Incidents WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadIncident(reader) : null;
        }

        public int CreateIncident(Incident incident)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Incidents (Number, Author, Department, Category, Description, Priority, CreatedAt, ReactionDeadline, ResolutionDeadline, AssignedTo, Status)
                VALUES ($num, $author, $dept, $cat, $desc, $pri, $created, $react, $resolve, $assigned, $status);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$num", incident.Number);
            cmd.Parameters.AddWithValue("$author", incident.Author);
            cmd.Parameters.AddWithValue("$dept", incident.Department);
            cmd.Parameters.AddWithValue("$cat", incident.Category);
            cmd.Parameters.AddWithValue("$desc", incident.Description);
            cmd.Parameters.AddWithValue("$pri", (int)incident.Priority);
            cmd.Parameters.AddWithValue("$created", incident.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$react", incident.ReactionDeadline?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$resolve", incident.ResolutionDeadline?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$assigned", incident.AssignedTo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$status", (int)incident.Status);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdateIncident(Incident incident)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Incidents SET Author=$author, Department=$dept, Category=$cat, Description=$desc,
                Priority=$pri, ReactionDeadline=$react, ResolutionDeadline=$resolve,
                AssignedTo=$assigned, Status=$status WHERE Id=$id";
            cmd.Parameters.AddWithValue("$author", incident.Author);
            cmd.Parameters.AddWithValue("$dept", incident.Department);
            cmd.Parameters.AddWithValue("$cat", incident.Category);
            cmd.Parameters.AddWithValue("$desc", incident.Description);
            cmd.Parameters.AddWithValue("$pri", (int)incident.Priority);
            cmd.Parameters.AddWithValue("$react", incident.ReactionDeadline?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$resolve", incident.ResolutionDeadline?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$assigned", incident.AssignedTo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$status", (int)incident.Status);
            cmd.Parameters.AddWithValue("$id", incident.Id);
            cmd.ExecuteNonQuery();
        }

        public string GenerateNumber()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Incidents";
            var count = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
            return $"INC-{DateTime.Now:yyyyMM}-{count:D4}";
        }

        private static Incident ReadIncident(SqliteDataReader r) => new()
        {
            Id = r.GetInt32(0),
            Number = r.GetString(1),
            Author = r.GetString(2),
            Department = r.GetString(3),
            Category = r.GetString(4),
            Description = r.GetString(5),
            Priority = (Priority)r.GetInt32(6),
            CreatedAt = DateTime.Parse(r.GetString(7)),
            ReactionDeadline = r.IsDBNull(8) ? null : DateTime.Parse(r.GetString(8)),
            ResolutionDeadline = r.IsDBNull(9) ? null : DateTime.Parse(r.GetString(9)),
            AssignedTo = r.IsDBNull(10) ? null : r.GetString(10),
            Status = (IncidentStatus)r.GetInt32(11)
        };

        //history

        public void AddHistory(HistoryEntry entry)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO History (IncidentId, Timestamp, User, Action, OldValue, NewValue) VALUES ($iid, $ts, $user, $action, $old, $new)";
            cmd.Parameters.AddWithValue("$iid", entry.IncidentId);
            cmd.Parameters.AddWithValue("$ts", entry.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("$user", entry.User);
            cmd.Parameters.AddWithValue("$action", entry.Action);
            cmd.Parameters.AddWithValue("$old", entry.OldValue ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$new", entry.NewValue ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public List<HistoryEntry> GetHistory(int incidentId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM History WHERE IncidentId=$id ORDER BY Timestamp DESC";
            cmd.Parameters.AddWithValue("$id", incidentId);
            var list = new List<HistoryEntry>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new HistoryEntry
            {
                Id = r.GetInt32(0), IncidentId = r.GetInt32(1),
                Timestamp = DateTime.Parse(r.GetString(2)),
                User = r.GetString(3), Action = r.GetString(4),
                OldValue = r.IsDBNull(5) ? null : r.GetString(5),
                NewValue = r.IsDBNull(6) ? null : r.GetString(6)
            });
            return list;
        }

        // comments

        public void AddComment(Comment comment)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Comments (IncidentId, Timestamp, Author, Text) VALUES ($iid, $ts, $author, $text)";
            cmd.Parameters.AddWithValue("$iid", comment.IncidentId);
            cmd.Parameters.AddWithValue("$ts", comment.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("$author", comment.Author);
            cmd.Parameters.AddWithValue("$text", comment.Text);
            cmd.ExecuteNonQuery();
        }

        public List<Comment> GetComments(int incidentId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Comments WHERE IncidentId=$id ORDER BY Timestamp ASC";
            cmd.Parameters.AddWithValue("$id", incidentId);
            var list = new List<Comment>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Comment
            {
                Id = r.GetInt32(0), IncidentId = r.GetInt32(1),
                Timestamp = DateTime.Parse(r.GetString(2)),
                Author = r.GetString(3), Text = r.GetString(4)
            });
            return list;
        }

        // attachments

        public void AddAttachment(Attachment attachment)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Attachments (IncidentId, FileName, FilePath, FileSize, UploadedAt) VALUES ($iid, $fn, $fp, $fs, $ua)";
            cmd.Parameters.AddWithValue("$iid", attachment.IncidentId);
            cmd.Parameters.AddWithValue("$fn", attachment.FileName);
            cmd.Parameters.AddWithValue("$fp", attachment.FilePath);
            cmd.Parameters.AddWithValue("$fs", attachment.FileSize);
            cmd.Parameters.AddWithValue("$ua", attachment.UploadedAt.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public List<Attachment> GetAttachments(int incidentId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Attachments WHERE IncidentId=$id";
            cmd.Parameters.AddWithValue("$id", incidentId);
            var list = new List<Attachment>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Attachment
            {
                Id = r.GetInt32(0), IncidentId = r.GetInt32(1),
                FileName = r.GetString(2), FilePath = r.GetString(3),
                FileSize = r.GetInt64(4), UploadedAt = DateTime.Parse(r.GetString(5))
            });
            return list;
        }

        public void DeleteAttachment(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Attachments WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        // reports

        public Dictionary<string, int> GetStatusReport()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Status, COUNT(*) FROM Incidents GROUP BY Status";
            var result = new Dictionary<string, int>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
                result[StatusHelper.ToRussian((IncidentStatus)r.GetInt32(0))] = r.GetInt32(1);
            return result;
        }

        public Dictionary<string, int> GetCategoryReport()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Category, COUNT(*) FROM Incidents GROUP BY Category ORDER BY COUNT(*) DESC";
            var result = new Dictionary<string, int>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) result[r.GetString(0)] = r.GetInt32(1);
            return result;
        }

        public List<Incident> GetOverdueIncidents()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Incidents WHERE ResolutionDeadline IS NOT NULL AND Status NOT IN (3,4,5)";
            var now = DateTime.Now.ToString("o");
            var list = new List<Incident>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var inc = ReadIncident(r);
                if (inc.IsOverdue) list.Add(inc);
            }
            return list;
        }
    }
}
