using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class AdminComplete
    {
        public int ID { get; set; }

        public int SessionClientID { get; set; }

        [Display(Name = "Has session been paid for?")]
        public bool? IsPaid { get; set; }

        [Display(Name = "Admin Notes")]
        [StringLength(500, ErrorMessage = "Admin Notes cannot exceed 500 characters.")]
        public string? AdminNotes { get; set; }

        [Display(Name = "Admin Initials")]
        [StringLength(5, ErrorMessage = "Admin Initials cannot exceed 5 characters.")]
        public string? AdminInitials { get; set; }

        public SessionClient? SessionClient { get; set; }
    }
}
