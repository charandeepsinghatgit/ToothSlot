using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ToothSlot.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Appointment>? PatientAppointments { get; set; }
        public virtual DentistProfile? DentistProfile { get; set; }
    }
}