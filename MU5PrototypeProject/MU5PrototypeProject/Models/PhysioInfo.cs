
using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class PhysioInfo
    {
        public int ID { get; set; }

        //“This child entity might not have its Session object loaded/assigned right now.”
        public int SessionID { get; set; }
        public Session? Session { get; set; } = null!;

        [Display(Name = "Physio Assessment PDF")]
        [StringLength(2048)]
        [DataType(DataType.Url)]
        [Required(ErrorMessage = "You must enter a physio assessment PDF")]
        public string? PhysioAssessment { get; set; }

        [Display(Name = "Insurance Company")]
        // Prototype mode: keep physio fields optional until role-aware session workflows are added.
        [Required(ErrorMessage = "You must specify the Insurance Company name.")]
        [StringLength(100)]
        public string? InsuranceCompany { get; set; }

        [Display(Name = "Coverage Amount/Year")]
        [Required(ErrorMessage = "You must enter the Coverage Amount per Year.")]
        [DataType(DataType.Currency)]
        public decimal? CoverageAmountPerYear { get; set; }

        [Display(Name = "Amount Used")]
        [Required(ErrorMessage = "You must specify the Amount Used.")]
        [DataType(DataType.Currency)]
        public decimal? AmountUsed { get; set; }

        [Display(Name = "Coverage Reset Date (YYYY-MM-DD)")]
        [Required(ErrorMessage = "You must specify the Coverage Resets Date.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? CoverageResetsDate { get; set; }

        [Display(Name = "Physiotherapist Name")]
        [Required(ErrorMessage = "You must specify the Physiotherapist Name.")]
        [StringLength(100, ErrorMessage = "Physiotherapist Name cannot be more than 100 characters long.")]
        [RegularExpression(@"^[A-Za-z\s.-]+$", ErrorMessage = "Only letters, spaces, hyphens, and periods allowed.")] public string? PhysiotherapistName { get; set; }

        [Display(Name = "Coverage Shared")]
        public bool CoverageShared { get; set; }

        [Display(Name = "Communicated with Physio")]
        public bool CommunicatedWithPhysio { get; set; }
    }
}
