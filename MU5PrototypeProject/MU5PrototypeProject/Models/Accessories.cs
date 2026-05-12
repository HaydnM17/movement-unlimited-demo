using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Models
{
    public enum HeadPadOption
    {
        Down = 1,
        Middle,
        Full,
        [Display(Name = "1 Extra Cushion")]
        OneExtraCushion,
        [Display(Name = "2 Extra Cushion")]
        TwoExtraCushion,
        [Display(Name = "Posture Pillow")]
        PosturePillow
    }

    public enum StrapOrHandleOption
    {
        Straps = 1,
        Handles = 2
    }

    public class Accessories
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "A session participant must be associated with these accessories.")]
        public int SessionClientID { get; set; }
        public SessionClient? SessionClient { get; set; }

        [Display(Name = "Head Rest")]
        [Required(ErrorMessage = "You must select a head rest option.")]
        public HeadPadOption HeadPad { get; set; }

        [Display(Name = "Straps/Handles")]
        [Required(ErrorMessage = "You must select a straps or handles option.")]
        public StrapOrHandleOption StrapsOrHandles { get; set; }

        [Display(Name = "Gear Bar")]
        [Range(1, 3, ErrorMessage = "Gear Bar must be between 1 and 3.")]
        public int GearBar { get; set; }

        [Display(Name = "Stopper Settings")]
        [Range(1, 6, ErrorMessage = "Stopper settings must be between 1 and 6.")]
        public int StopperSettings { get; set; }

        [Display(Name = "Rubber Pads")]
        public bool RubberPads { get; set; }

        [Display(Name = "Head Rest")]
        public bool HeadRest { get; set; }

        public bool Towel { get; set; }

        [Display(Name = "Posture Pillow")]
        public bool PosturePillow { get; set; }

        public int? SpringID { get; set; }
        public Spring? Spring { get; set; }
    }
}
