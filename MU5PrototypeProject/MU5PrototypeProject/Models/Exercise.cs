using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class Exercise
    {
        public int ID { get; set; }

        [Display(Name = "Exercise Name")]
        [Required(ErrorMessage = "You must enter an exercise name.")]
        [StringLength(100, ErrorMessage = "Exercise name cannot exceed 100 characters.")]
        public string ExerciseName { get; set; } = string.Empty;

        [Display(Name = "Apparatus")]
        public int? ApparatusID { get; set; }
        public Apparatus? Apparatus { get; set; }

        public ICollection<Action> Actions { get; set; } = new HashSet<Action>();
    }
}