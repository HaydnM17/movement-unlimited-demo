using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class Trainer
    {
        public int ID { get; set; }

        [Display(Name = "Trainer Name")]
        public string TrainerName => $"{FirstName} {LastName}";

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "You cannot leave the first name blank.")]
        [StringLength(50, ErrorMessage = "First name cannot be more than 50 characters long.")]
        [RegularExpression(@"^[A-Za-z-]+$", ErrorMessage = "First name can only contain letters and hyphens.")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "You cannot leave the last name blank.")]
        [StringLength(100, ErrorMessage = "Last name cannot be more than 100 characters long.")]
        [RegularExpression(@"^[A-Za-z-]+$", ErrorMessage = "Last name can only contain letters and hyphens.")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [Display(Name = "Role")]
        [StringLength(50)]
        public string? Role { get; set; }

        [StringLength(450)]
        public string? ApplicationUserId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public ICollection<Session> Sessions { get; set; } = new HashSet<Session>();
    }
}
