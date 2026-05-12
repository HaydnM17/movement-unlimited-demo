using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models.ViewModels
{
    public class ActionEditVM : ActionCreateVM
    {
        [Required]
        public int ActionID { get; set; }
    }
}