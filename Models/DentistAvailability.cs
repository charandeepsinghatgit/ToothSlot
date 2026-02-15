using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToothSlot.Models
{
    public class DentistAvailability
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DentistId { get; set; } = string.Empty;

        [Required]
        [Range(0, 6)]
        public int DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("DentistId")]
        public virtual ApplicationUser Dentist { get; set; } = null!;
    }
}