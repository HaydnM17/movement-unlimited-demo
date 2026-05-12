using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}
