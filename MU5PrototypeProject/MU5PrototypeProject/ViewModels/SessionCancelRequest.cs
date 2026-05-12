using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models.ViewModels
{
    public class SessionCancelRequest : IValidatableObject
    {
        public int Id { get; set; }

        [Display(Name = "Cancellation Reason")]
        [Required(ErrorMessage = "A cancellation reason is required.")]
        [StringLength(500, ErrorMessage = "Cancellation reason cannot be longer than 500 characters.")]
        public string CancellationReason { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(CancellationReason))
            {
                yield return new ValidationResult(
                    "A cancellation reason is required.",
                    new[] { nameof(CancellationReason) });
            }
        }
    }
}
