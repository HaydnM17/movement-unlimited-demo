using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MU5PrototypeProject.Models;

namespace MU5PrototypeProject.ViewModels
{
    /// <summary>
    /// ViewModel for creating a Session with nested Actions
    /// </summary>
    public class SessionCreateVM
    {
        public int ID { get; set; }

        [Display(Name = "Session Date (YYYY-MM-DD)")]
        [Required(ErrorMessage = "You must specify the date for the session.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime SessionDate { get; set; }

        [Display(Name = "Sessions/Week")]
        [Required(ErrorMessage = "You must select a recommended Session per Week.")]
        public int? SessionsPerWeekRecommended { get; set; }

        [Display(Name = "Archived")]
        public bool IsArchived { get; set; }

        [Display(Name = "Session Type")]
        [Required(ErrorMessage = "You must select a session type.")]
        public SessionType SessionType { get; set; }

        public SessionStatus Status { get; set; } = SessionStatus.Opened;

        [Display(Name = "Trainer")]
        [Required(ErrorMessage = "You must select a trainer to add to the session.")]
        public int TrainerID { get; set; }

        [Display(Name = "Client")]
        [Required(ErrorMessage = "You must select a client to add to the session.")]
        public int ClientID { get; set; }

        public int? Client2ID { get; set; }

        // Session-related data
        public SessionNotes? SessionNotes { get; set; }
        public SessionNotes? Client2SessionNotes { get; set; }
        public NextSteps? NextSteps { get; set; }
        public NextSteps? Client2NextSteps { get; set; }
        public PhysioInfo? PhysioInfo { get; set; }
        public PhysioInfo? Client2PhysioInfo { get; set; }
        public AdminComplete? AdminComplete { get; set; }
        public AdminComplete? Client2AdminComplete { get; set; }
        public Accessories? Accessories { get; set; }

        // Selected Exercise IDs for creating Actions
        [NotMapped]
        [Display(Name = "Exercises")]
        public ICollection<int> SelectedExerciseIDs { get; set; } = new HashSet<int>();

        // For Client 2 in semi-private sessions
        public int? Client2SessionsPerWeekRecommended { get; set; }
    }
}
