using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MU5PrototypeProject.Models
{
    public enum SessionType
    {
        Private,
        SemiPrivate,
        Physio
    }

    public enum SessionStatus
    {
        Opened,
        Logged,
        Completed
    }

    public class Session : Auditable
    {
        public int ID { get; set; }

        [Display(Name = "Session Date (YYYY-MM-DD)")]
        [Required(ErrorMessage = "You must specify the date for the session.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime SessionDate { get; set; }

        [Display(Name = "Archived")]
        public bool IsArchived { get; set; }

        [Display(Name = "Canceled")]
        public bool IsCanceled { get; set; }

        [Display(Name = "Cancellation Reason")]
        [StringLength(500)]
        public string? CancellationReason { get; set; }

        [Display(Name = "Canceled On")]
        public DateTime? CanceledOn { get; set; }

        [Display(Name = "Canceled By")]
        [StringLength(256)]
        public string? CanceledBy { get; set; }

        [Display(Name = "Session Type")]
        [Required(ErrorMessage = "You must select a session type.")]
        public SessionType SessionType { get; set; }

        public SessionStatus Status { get; set; } = SessionStatus.Opened;

        [NotMapped]
        [Display(Name = "Action")]
        public IEnumerable<Action> Actions =>
            OrderedSessionClients
                .SelectMany(sc => sc.Actions)
                .OrderBy(a => a.ID);

        [NotMapped]
        [Display(Name = "Exercises")]
        public ICollection<int> SelectedExerciseIDs { get; set; } = new HashSet<int>();

        [Display(Name = "Physio Information")]
        public PhysioInfo? PhysioInfo { get; set; }

        [Display(Name = "Teacher")]
        [Required(ErrorMessage = "You must select a teacher to add to the session.")]
        public int TrainerID { get; set; }
        public Trainer? Trainer { get; set; }

        public ICollection<SessionClient> SessionClients { get; set; } = new HashSet<SessionClient>();

        [NotMapped]
        public IEnumerable<SessionClient> OrderedSessionClients =>
            SessionClients.OrderBy(sc => sc.ParticipantOrder);

        [NotMapped]
        public SessionClient? PrimarySessionClient =>
            OrderedSessionClients.FirstOrDefault(sc => sc.ParticipantOrder == 1);

        [NotMapped]
        public SessionClient? SecondarySessionClient =>
            OrderedSessionClients.FirstOrDefault(sc => sc.ParticipantOrder == 2);

        [NotMapped]
        public Client? Client => PrimarySessionClient?.Client;

        [NotMapped]
        public int? ClientID => PrimarySessionClient?.ClientID;

        [NotMapped]
        public int? Client2ID => SecondarySessionClient?.ClientID;

        [NotMapped]
        public int? SessionsPerWeekRecommended => PrimarySessionClient?.SessionsPerWeekRecommended;

        [NotMapped]
        public int? Client2SessionsPerWeekRecommended => SecondarySessionClient?.SessionsPerWeekRecommended;

        [NotMapped]
        public int? SharedSessionsPerWeekRecommended =>
            SessionsPerWeekRecommended ?? Client2SessionsPerWeekRecommended;

        [NotMapped]
        public SessionNotes? SessionNotes => PrimarySessionClient?.SessionNotes;

        [NotMapped]
        public NextSteps? NextSteps => PrimarySessionClient?.NextSteps;

        [NotMapped]
        public AdminComplete? AdminComplete => PrimarySessionClient?.AdminComplete;

        [NotMapped]
        public SessionNotes? Client2SessionNotes => SecondarySessionClient?.SessionNotes;

        [NotMapped]
        public NextSteps? Client2NextSteps => SecondarySessionClient?.NextSteps;

        [NotMapped]
        public AdminComplete? Client2AdminComplete => SecondarySessionClient?.AdminComplete;

        [NotMapped]
        public string ParticipantNames =>
            string.Join(" / ",
                OrderedSessionClients
                    .Select(sc => sc.Client?.ClientName)
                    .Where(name => !string.IsNullOrWhiteSpace(name)));

        [NotMapped]
        public string SharedSessionsPerWeekDisplay =>
            SharedSessionsPerWeekRecommended?.ToString() ?? "-";

        [NotMapped]
        public string SessionsPerWeekSummary =>
            string.Join(" / ",
                OrderedSessionClients.Select(sc =>
                    sc.SessionsPerWeekRecommended?.ToString() ?? "-"));

        [NotMapped]
        public int PrimaryCompletedSessionsCount { get; set; }

        [NotMapped]
        public int SecondaryCompletedSessionsCount { get; set; }

        [NotMapped]
        public string TotalCompletedSessionsDisplay =>
            OrderedSessionClients.Any()
                ? string.Join(" / ",
                    OrderedSessionClients.Select(sc =>
                        (sc.ParticipantOrder == 2
                            ? SecondaryCompletedSessionsCount
                            : PrimaryCompletedSessionsCount).ToString()))
                : "-";

    }
}
