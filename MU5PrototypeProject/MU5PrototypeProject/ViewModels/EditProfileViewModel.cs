using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required]
        [Display(Name = "First name")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last name")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
