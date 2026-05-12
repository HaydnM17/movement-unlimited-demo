namespace MU5PrototypeProject.Models.ViewModels
{
    public class SessionHistoryActionDto
    {
        public int Id { get; set; }
        public string ExerciseName { get; set; } = "";
        public string? ApparatusName { get; set; }
        public string? Springs { get; set; }
        public List<string> PropNames { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class SessionHistoryDto
    {
        public int SessionId { get; set; }
        public string SessionDate { get; set; } = "";
        public int Offset { get; set; }
        public int TotalCount { get; set; }
        public int SessionTypeValue { get; set; }
        public string SessionTypeText { get; set; } = "";
        public int? TrainerId { get; set; }
        public string TrainerName { get; set; } = "";
        public int? ClientId { get; set; }
        public string ClientName { get; set; } = "";
        public int? Client2Id { get; set; }
        public string Client2Name { get; set; } = "";
        public List<SessionHistoryActionDto> Actions { get; set; } = new();
        public int? SessionsPerWeekRecommended { get; set; }

        // SessionNotes
        public string? Goals { get; set; }
        public string? GeneralComments { get; set; }
        public string? SubjectiveReports { get; set; }
        public string? ObjectiveFindings { get; set; }
        public string? Plan { get; set; }

        // NextSteps
        public bool NextAppointmentBooked { get; set; }
        public bool CommunicatedProgress { get; set; }
        public bool ReadyToProgress { get; set; }
        public bool CourseCorrectionNeeded { get; set; }
        public bool TeamConsult { get; set; }
        public bool ReferredExternally { get; set; }
        public string? ReferredTo { get; set; }

        // AdminComplete
        public bool? IsPaid { get; set; }
        public string? AdminNotes { get; set; }
        public string? AdminInitials { get; set; }

        // Accessories (nullable — session may not have them)
        public string? HeadPad { get; set; }
        public string? StrapsOrHandles { get; set; }
        public int? GearBar { get; set; }
        public int? StopperSettings { get; set; }
        public bool? RubberPads { get; set; }
        public bool? HeadRest { get; set; }
        public bool? Towel { get; set; }
        public bool? PosturePillow { get; set; }

        // PhysioInfo (nullable — session may not have it)
        public string? PhysioAssessment { get; set; }
        public string? InsuranceCompany { get; set; }
        public decimal? CoverageAmountPerYear { get; set; }
        public decimal? AmountUsed { get; set; }
        public string? CoverageResetsDate { get; set; }
        public string? PhysiotherapistName { get; set; }
        public bool? CoverageShared { get; set; }
        public bool? CommunicatedWithPhysio { get; set; }
    }
}
