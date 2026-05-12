using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public class SessionNotes
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "A session participant must be associated with these notes.")]
        public int SessionClientID { get; set; }
        public SessionClient? SessionClient { get; set; }

        [Display(Name = "Goals")]
        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "Goals cannot exceed 1000 characters.")]
        public string? Goals { get; set; }

        [Display(Name = "General Comments")]
        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "General comments cannot exceed 1000 characters.")]
        public string? GeneralComments { get; set; }

        [Display(Name = "Subjective Reports")]
        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "Subjective reports cannot exceed 1000 characters.")]
        public string? SubjectiveReports { get; set; }

        [Display(Name = "Objective Findings")]
        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "Objective findings cannot exceed 1000 characters.")]
        public string? ObjectiveFindings { get; set; }

        [Display(Name = "Plan")]
        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "Plan cannot exceed 1000 characters.")]
        public string? Plan { get; set; }

        public int? CompletedByTrainerID { get; set; }
        public Trainer? CompletedByTrainer { get; set; }
    }
}
