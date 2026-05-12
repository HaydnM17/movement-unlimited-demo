using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{// it now serves the purpose of ActionProp, I just dont want to change the name
    public class ExerciseProp
    {
        public int ID { get; set; }

        [Required]
        public int ActionID { get; set; }
        public Action Action { get; set; } = null!;

        [Required]
        public int PropID { get; set; }
        public Prop Prop { get; set; } = null!;
    }
}