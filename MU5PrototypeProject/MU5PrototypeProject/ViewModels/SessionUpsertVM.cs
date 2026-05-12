using System.ComponentModel.DataAnnotations;
using MU5PrototypeProject.Models;

namespace MU5PrototypeProject.Models.ViewModels
{
    public class SessionUpsertVM : IValidatableObject
    {
        public int ID { get; set; }

        public int? PrimarySessionClientID { get; set; }
        public int? SecondarySessionClientID { get; set; }

        [Display(Name = "Session Date (YYYY-MM-DD)")]
        [Required(ErrorMessage = "You must specify the date for the session.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime SessionDate { get; set; } = DateTime.Today;

        [Display(Name = "Archived")]
        public bool IsArchived { get; set; }

        [Display(Name = "Session Type")]
        [Required(ErrorMessage = "You must select a session type.")]
        public SessionType SessionType { get; set; }

        public SessionStatus CurrentStatus { get; set; } = SessionStatus.Opened;

        public SessionStatus Status { get; set; } = SessionStatus.Opened;

        [Display(Name = "Trainer")]
        [Required(ErrorMessage = "You must select a trainer to add to the session.")]
        public int? TrainerID { get; set; }

        [Display(Name = "Client")]
        [Required(ErrorMessage = "You must select a client to add to the session.")]
        public int? ClientID { get; set; }

        [Display(Name = "Sessions/Week")]
        [Range(1, int.MaxValue, ErrorMessage = "Sessions/Week must be at least 1.")]
        public int? SessionsPerWeekRecommended { get; set; }

        public SessionNotes SessionNotes { get; set; } = new();
        public NextSteps NextSteps { get; set; } = new();
        public AdminComplete AdminComplete { get; set; } = new();

        public int? Client2ID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sessions/Week must be at least 1.")]
        public int? Client2SessionsPerWeekRecommended { get; set; }

        public SessionNotes? Client2SessionNotes { get; set; }
        public NextSteps? Client2NextSteps { get; set; }
        public AdminComplete? Client2AdminComplete { get; set; }
        public Accessories? Client2Accessories { get; set; }

        public PhysioInfo? PhysioInfo { get; set; }
        public Accessories? Accessories { get; set; } = new();

        [Display(Name = "Exercises")]
        public ICollection<int> SelectedExerciseIDs { get; set; } = new HashSet<int>();

        public ICollection<MU5PrototypeProject.Models.Action> ExistingActions { get; set; } =
            new List<MU5PrototypeProject.Models.Action>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SessionType == SessionType.SemiPrivate)
            {
                if (!Client2ID.HasValue)
                {
                    yield return new ValidationResult(
                        "You must select a second client for a semi-private session.",
                        new[] { nameof(Client2ID) });
                }

                if (Client2ID.HasValue && ClientID.HasValue && Client2ID.Value == ClientID.Value)
                {
                    yield return new ValidationResult(
                        "Client 2 must be different from Client 1.",
                        new[] { nameof(Client2ID), nameof(ClientID) });
                }
            }
            else if (Client2ID.HasValue)
            {
                yield return new ValidationResult(
                    "Second client is only allowed for semi-private sessions.",
                    new[] { nameof(Client2ID) });
            }
        }
    }
}
