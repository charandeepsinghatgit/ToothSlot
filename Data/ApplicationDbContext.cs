using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ToothSlot.Models;

namespace ToothSlot.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DentistProfile> DentistProfiles { get; set; }
        public DbSet<DentalService> DentalServices { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<DentistAvailability> DentistAvailabilities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.DentistProfile)
                .WithOne(dp => dp.User)
                .HasForeignKey<DentistProfile>(dp => dp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(u => u.PatientAppointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Dentist)
                .WithMany()
                .HasForeignKey(a => a.DentistId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DentistAvailability>()
                .HasOne(da => da.Dentist)
                .WithMany()
                .HasForeignKey(da => da.DentistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DentalService>()
                .Property(s => s.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.AppointmentDate);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.Status);

            modelBuilder.Entity<DentistAvailability>()
                .HasIndex(da => new { da.DentistId, da.DayOfWeek });
        }
    }
}