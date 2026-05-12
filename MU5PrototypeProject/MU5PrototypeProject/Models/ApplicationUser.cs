using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool MustChangePassword { get; set; } = true;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
