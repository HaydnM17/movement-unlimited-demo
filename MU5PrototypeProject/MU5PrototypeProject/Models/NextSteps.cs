using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class NextSteps : IValidatableObject
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "A session participant must be associated with these next steps.")]
        public int SessionClientID { get; set; }
        public SessionClient? SessionClient { get; set; }

        [Display(Name = "Next Appointment Booked")]
        public bool NextAppointmentBooked { get; set; }

        [Display(Name = "Communicated Progress")]
        public bool CommunicatedProgress { get; set; }

        [Display(Name = "Ready to Progress")]
        public bool ReadyToProgress { get; set; }

        [Display(Name = "Course Correction Needed")]
        public bool CourseCorrectionNeeded { get; set; }

        [Display(Name = "Team Consult")]
        public bool TeamConsult { get; set; }

        [Display(Name = "Referred Externally")]
        public bool ReferredExternally { get; set; }

        [Display(Name = "Referred To")]
        [StringLength(100, ErrorMessage = "Referred To cannot exceed 100 characters.")]
        public string? ReferredTo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ReferredExternally && string.IsNullOrWhiteSpace(ReferredTo))
            {
                yield return new ValidationResult(
                    "You must specify who the client was referred to.",
                    new[] { nameof(ReferredTo) });
            }
        }
    }
}
