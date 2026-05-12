using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models.ViewModels
{
    public class ActionCreateVM
    {
        public int SessionID { get; set; }

        [Range(1, int.MaxValue)]
        public int SessionClientID { get; set; }

        [Required]
        [Display(Name = "Exercise")]
        public int ExerciseID { get; set; }

        [Display(Name = "Exercise Type")]
        [Required(ErrorMessage = "An Exercise type must be selected.")]
        public ActionType ActionType { get; set; } = ActionType.MidSession;

        public string? Springs { get; set; }
        public string? Notes { get; set; }

        public int? ApparatusFilterID { get; set; }

        // multi-select props
        public List<int> SelectedPropIDs { get; set; } = new();

        // dropdown data
        public SelectList? ExerciseList { get; set; }
        public MultiSelectList? PropList { get; set; }
        public SelectList? SpringList { get; set; }
    }
}
