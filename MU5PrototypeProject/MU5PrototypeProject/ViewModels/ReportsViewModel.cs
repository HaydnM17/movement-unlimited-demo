namespace MU5PrototypeProject.ViewModels
{
    public class DateRangeFilter
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Preset { get; set; } = "ThisMonth";
    }

    public class SessionsByDayRow
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Private { get; set; }
        public int SemiPrivate { get; set; }
        public int Physio { get; set; }
    }

    public class SessionStatistics
    {
        public int TotalSessions { get; set; }
        public int PrivateCount { get; set; }
        public int SemiPrivateCount { get; set; }
        public int PhysioCount { get; set; }
        public List<SessionsByDayRow> ByDayOfWeek { get; set; } = new();
    }

    public class ClientActivityRow
    {
        public string ClientName { get; set; } = string.Empty;
        public int SessionCount { get; set; }
        public DateTime? LastSessionDate { get; set; }
        public string MostFrequentType { get; set; } = string.Empty;
    }

    public class ClientAnalytics
    {
        public int TotalActiveClients { get; set; }
        public double AvgSessionsPerClient { get; set; }
        public List<ClientActivityRow> MostActiveClients { get; set; } = new();
    }

    public class TrainerWorkloadRow
    {
        public string TrainerName { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public int UniqueClients { get; set; }
        public int PrivateCount { get; set; }
        public int SemiPrivateCount { get; set; }
        public int PhysioCount { get; set; }
    }

    public class TrainerWorkload
    {
        public List<TrainerWorkloadRow> Trainers { get; set; } = new();
    }

    public class ExercisePopularityRow
    {
        public string ExerciseName { get; set; } = string.Empty;
        public string ApparatusName { get; set; } = string.Empty;
        public int TimesUsed { get; set; }
        public int UniqueClients { get; set; }
    }

    public class ApparatusUsageRow
    {
        public string ApparatusName { get; set; } = string.Empty;
        public int ExerciseCount { get; set; }
        public int TotalUsages { get; set; }
    }

    public class PropUsageRow
    {
        public string PropName { get; set; } = string.Empty;
        public int TimesUsed { get; set; }
    }

    public class ExercisePopularity
    {
        public List<ExercisePopularityRow> TopExercises { get; set; } = new();
        public List<ApparatusUsageRow> ApparatusUsage { get; set; } = new();
        public List<PropUsageRow> PropUsage { get; set; } = new();
    }

    public class ReportsViewModel
    {
        public DateRangeFilter Filter { get; set; } = new();
        public SessionStatistics SessionStats { get; set; } = new();
        public ClientAnalytics ClientAnalytics { get; set; } = new();
        public TrainerWorkload TrainerWorkload { get; set; } = new();
        public ExercisePopularity ExercisePopularity { get; set; } = new();
    }
}
