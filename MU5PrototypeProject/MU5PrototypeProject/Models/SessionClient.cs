using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class SessionClient : IValidatableObject
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "A session must be associated with this participant.")]
        public int SessionID { get; set; }
        public Session? Session { get; set; }

        [Display(Name = "Client")]
        [Required(ErrorMessage = "You must select a client to add to the session.")]
        public int ClientID { get; set; }
        public Client? Client { get; set; }

        [Display(Name = "Participant Order")]
        [Range(1, 2, ErrorMessage = "Participant order must be 1 or 2.")]
        public int ParticipantOrder { get; set; }

        [Display(Name = "Sessions/Week")]
        [Range(1, int.MaxValue, ErrorMessage = "Sessions/Week must be at least 1.")]
        public int? SessionsPerWeekRecommended { get; set; }

        [Display(Name = "Teacher Notes")]
        public SessionNotes? SessionNotes { get; set; }

        [Display(Name = "Next Steps")]
        public NextSteps? NextSteps { get; set; }

        [Display(Name = "Admin Completion")]
        public AdminComplete? AdminComplete { get; set; }

        [Display(Name = "Accessories")]
        public Accessories? Accessories { get; set; }

        [Display(Name = "Actions")]
        public ICollection<Action> Actions { get; set; } = new HashSet<Action>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ParticipantOrder is < 1 or > 2)
            {
                yield return new ValidationResult(
                    "Participant order must be 1 or 2.",
                    new[] { nameof(ParticipantOrder) });
            }
        }
    }
}
