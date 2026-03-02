using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToothSlot.Data;
using ToothSlot.Models;
using ToothSlot.ViewModels;

namespace ToothSlot.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        
        public AdminController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Dentists
        public async Task<IActionResult> Dentists()
        {
            var dentists = await _userManager.GetUsersInRoleAsync("Dentist");
            var dentistProfiles = await _context.DentistProfiles
                .Include(d => d.User)
                .ToListAsync();
            
            return View(dentists);
        }

        // GET: Admin/CreateDentist
        public IActionResult CreateDentist()
        {
            return View();
        }

        // POST: Admin/CreateDentist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDentist(CreateDentistViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add to Dentist role
                    await _userManager.AddToRoleAsync(user, "Dentist");

                    // Create dentist profile
                    var dentistProfile = new DentistProfile
                    {
                        UserId = user.Id,
                        Specialization = model.Specialization,
                        Bio = model.Bio,
                        IsActive = true
                    };

                    _context.DentistProfiles.Add(dentistProfile);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Dentist Dr. {model.FirstName} {model.LastName} created successfully!";
                    return RedirectToAction(nameof(Dentists));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Admin/DentistDetails/5
        public async Task<IActionResult> DentistDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var dentist = await _userManager.FindByIdAsync(id);
            if (dentist == null)
            {
                return NotFound();
            }

            var dentistProfile = await _context.DentistProfiles
                .FirstOrDefaultAsync(d => d.UserId == id);

            var viewModel = new DentistDetailsViewModel
            {
                Id = dentist.Id,
                FirstName = dentist.FirstName,
                LastName = dentist.LastName,
                Email = dentist.Email ?? "",
                PhoneNumber = dentist.PhoneNumber,
                Specialization = dentistProfile?.Specialization ?? "Not specified",
                Bio = dentistProfile?.Bio,
                IsActive = dentistProfile?.IsActive ?? true
            };

            return View(viewModel);
        }

        // POST: Admin/DeleteDentist/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDentist(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
        
            var dentist = await _userManager.FindByIdAsync(id);
            if (dentist == null)
            {
                TempData["Error"] = "Dentist not found.";
                return RedirectToAction(nameof(Dentists));
            }
        
            // Check if dentist has any appointments
            var hasAppointments = await _context.Appointments
                .AnyAsync(a => a.DentistId == id  && a.Status != "Cancelled");
        
            if (hasAppointments)
            {
                TempData["Error"] = $"Cannot delete Dr. {dentist.FirstName} {dentist.LastName}. This dentist has appointment records.";
                return RedirectToAction(nameof(Dentists));
            }
        
            // Delete dentist profile first
            var dentistProfile = await _context.DentistProfiles
                .FirstOrDefaultAsync(d => d.UserId == id);
            
            if (dentistProfile != null)
            {
                _context.DentistProfiles.Remove(dentistProfile);
                await _context.SaveChangesAsync();
            }
        
            // Delete user account
            var result = await _userManager.DeleteAsync(dentist);
        
            if (result.Succeeded)
            {
                TempData["Success"] = $"Dentist Dr. {dentist.FirstName} {dentist.LastName} has been deleted successfully.";
            }
            else
            {
                TempData["Error"] = "An error occurred while deleting the dentist.";
            }
        
            return RedirectToAction(nameof(Dentists));
        }
    }
}
