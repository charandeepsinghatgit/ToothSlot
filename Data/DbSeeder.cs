using Microsoft.AspNetCore.Identity;
using ToothSlot.Models;

namespace ToothSlot.Data
{
    public class DbSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbSeeder(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            await CreateRolesAsync();
            await SeedServicesAsync();
            await SeedUsersAsync();
        }

        private async Task CreateRolesAsync()
        {
            string[] roles = { "Admin", "Dentist", "Patient" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task SeedServicesAsync()
        {
            if (!_context.DentalServices.Any())
            {
                var services = new List<DentalService>
                {
                    new DentalService
                    {
                        Name = "Dental Cleaning",
                        Description = "Professional teeth cleaning and polishing",
                        Price = 120.00m,
                        DurationMinutes = 30,
                        IsActive = true
                    },
                    new DentalService
                    {
                        Name = "Root Canal",
                        Description = "Root canal therapy to save infected tooth",
                        Price = 850.00m,
                        DurationMinutes = 90,
                        IsActive = true
                    },
                    new DentalService
                    {
                        Name = "Dental Filling",
                        Description = "Composite or amalgam filling for cavities",
                        Price = 200.00m,
                        DurationMinutes = 45,
                        IsActive = true
                    },
                    new DentalService
                    {
                        Name = "Teeth Whitening",
                        Description = "Professional teeth whitening treatment",
                        Price = 350.00m,
                        DurationMinutes = 60,
                        IsActive = true
                    }
                };

                await _context.DentalServices.AddRangeAsync(services);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedUsersAsync()
        {
            // Seed Admin
            if (await _userManager.FindByEmailAsync("admin@toothslot.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@toothslot.com",
                    Email = "admin@toothslot.com",
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };

                await _userManager.CreateAsync(admin, "Admin@123");
                await _userManager.AddToRoleAsync(admin, "Admin");
            }

            // Seed Dentists
            var dentists = new[]
            {
                new { Email = "dr.smith@toothslot.com", FirstName = "John", LastName = "Smith", Specialization = "General Dentistry" },
                new { Email = "dr.jones@toothslot.com", FirstName = "Sarah", LastName = "Jones", Specialization = "Orthodontics" }
            };

            foreach (var dentist in dentists)
            {
                if (await _userManager.FindByEmailAsync(dentist.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = dentist.Email,
                        Email = dentist.Email,
                        FirstName = dentist.FirstName,
                        LastName = dentist.LastName,
                        EmailConfirmed = true
                    };

                    await _userManager.CreateAsync(user, "Dentist@123");
                    await _userManager.AddToRoleAsync(user, "Dentist");

                    var profile = new DentistProfile
                    {
                        UserId = user.Id,
                        Specialization = dentist.Specialization,
                        Bio = $"Experienced dentist specializing in {dentist.Specialization}",
                        IsActive = true
                    };

                    await _context.DentistProfiles.AddAsync(profile);
                }
            }

            // Seed Test Patient
            if (await _userManager.FindByEmailAsync("patient@test.com") == null)
            {
                var patient = new ApplicationUser
                {
                    UserName = "patient@test.com",
                    Email = "patient@test.com",
                    FirstName = "Test",
                    LastName = "Patient",
                    PhoneNumber = "416-555-0100",
                    EmailConfirmed = true
                };

                await _userManager.CreateAsync(patient, "Patient@123");
                await _userManager.AddToRoleAsync(patient, "Patient");
            }

            await _context.SaveChangesAsync();
        }
    }
}