using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "The new password must be at least 12 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
