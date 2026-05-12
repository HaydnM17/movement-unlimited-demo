using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class Prop
    {
        public int ID { get; set; }

        [Display(Name = "Prop Name")]
        [Required]
        [StringLength(100)]
        public string PropName { get; set; } = string.Empty;

        public ICollection<ExerciseProp> ExerciseProps { get; set; } = new HashSet<ExerciseProp>();
    }
}