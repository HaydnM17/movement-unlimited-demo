using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class Spring
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "An apparatus must be associated with this spring.")]
        public int ApparatusID { get; set; }
        public Apparatus? Apparatus { get; set; }

        [Display(Name = "Spring Name")]
        [Required(ErrorMessage = "Spring name is required.")]
        [StringLength(100, ErrorMessage = "Spring name cannot exceed 100 characters.")]
        public string SpringName { get; set; } = string.Empty;

        [Display(Name = "Tension Level")]
        [StringLength(50, ErrorMessage = "Tension level cannot exceed 50 characters.")]
        public string? TensionLevel { get; set; }

        [Display(Name = "Colour")]
        [StringLength(50, ErrorMessage = "Colour cannot exceed 50 characters.")]
        public string? Color { get; set; }

        public ICollection<Accessories> Accessories { get; set; } = new HashSet<Accessories>();
    }
}