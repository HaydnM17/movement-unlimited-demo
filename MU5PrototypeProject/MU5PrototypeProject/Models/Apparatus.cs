using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class Apparatus
    {
        public int ID { get; set; }

        [Display(Name = "Apparatus Name")]
        [Required(ErrorMessage = "Apparatus name is required.")]
        [StringLength(100)]
        public string ApparatusName { get; set; } = string.Empty;

        public ICollection<Exercise> Exercises { get; set; } = new HashSet<Exercise>();
        public ICollection<Spring> Springs { get; set; } = new HashSet<Spring>();
    }
}