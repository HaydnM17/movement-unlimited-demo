using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models.ViewModels
{
    public class AccountCreateViewModel
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
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Active account")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Linked trainer record")]
        public int? TrainerId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Temporary password")]
        public string TemporaryPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm temporary password")]
        [Compare(nameof(TemporaryPassword), ErrorMessage = "The temporary password and confirmation password do not match.")]
        public string ConfirmTemporaryPassword { get; set; } = string.Empty;

        public IEnumerable<SelectListItem> AvailableRoles { get; set; } = [];

        public IEnumerable<SelectListItem> AvailableTrainers { get; set; } = [];
    }
}
