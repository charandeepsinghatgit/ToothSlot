using System.ComponentModel.DataAnnotations;

namespace ToothSlot.ViewModels
{
    public class CreateDentistViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Specialization")]
        [StringLength(100)]
        public string Specialization { get; set; } = string.Empty;

        [Display(Name = "Bio")]
        [StringLength(500)]
        public string? Bio { get; set; }
    }
}