using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MU5PrototypeProject.Models
{
    public enum ActionType
    {
        PreSession,
        MidSession,
        PostSession
    }

    public class Action
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "A participant must be associated with this Excercise.")]
        public int SessionClientID { get; set; }
        public SessionClient? SessionClient { get; set; }

        [Required(ErrorMessage = "An exercise must be selected for this Excercise.")]
        public int ExerciseID { get; set; }
        public Exercise? Exercise { get; set; }

        [Display(Name = "Excercise Type")]
        [Required(ErrorMessage = "An Excercise type must be selected.")]
        public ActionType ActionType { get; set; } = ActionType.MidSession;

        [Display(Name = "Spring Setting")]
        [StringLength(50, ErrorMessage = "Spring settings cannot exceed 50 characters.")]
        public string? Springs { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [NotMapped]
        public bool IsSelected { get; set; }

        public ICollection<ExerciseProp> ExerciseProps { get; set; } = new HashSet<ExerciseProp>();
    }
}
