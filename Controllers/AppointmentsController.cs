using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ToothSlot.Data;
using ToothSlot.Models;

namespace ToothSlot.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Appointments
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            
            if (User.IsInRole("Patient"))
            {
                // Show patient's appointments
                var appointments = await _context.Appointments
                    .Include(a => a.Service)
                    .Include(a => a.Dentist)
                    .Where(a => a.PatientId == user.Id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
                
                return View(appointments);
            }
            else if (User.IsInRole("Dentist"))
            {
                // Show dentist's appointments
                var appointments = await _context.Appointments
                    .Include(a => a.Service)
                    .Include(a => a.Patient)
                    .Where(a => a.DentistId == user.Id)
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.StartTime)
                    .ToListAsync();
                
                return View("DentistIndex", appointments);
            }
            
            return View(new List<Appointment>());
        }

        // GET: Appointments/Book
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Book()
        {
            // Get active services
            ViewBag.Services = new SelectList(
                await _context.DentalServices
                    .Where(s => s.IsActive)
                    .ToListAsync(), 
                "Id", "Name");
            
            // Get dentists (users with Dentist role)
            var dentists = await _userManager.GetUsersInRoleAsync("Dentist");
            ViewBag.Dentists = new SelectList(
                dentists.Select(d => new { 
                    Id = d.Id, 
                    Name = $"Dr. {d.FirstName} {d.LastName}" 
                }), 
                "Id", "Name");
            
            return View(new Appointment());
        }

        // POST: Appointments/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Book(Appointment appointment)
        {
            var user = await _userManager.GetUserAsync(User);
            
            // Set patient ID
            appointment.PatientId = user.Id;
            appointment.Status = "Pending";
            appointment.CreatedAt = DateTime.UtcNow;
            
            // Calculate end time based on service duration
            var service = await _context.DentalServices.FindAsync(appointment.ServiceId);
            if (service != null)
            {
                appointment.EndTime = appointment.StartTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));
            }
            
            // Check for conflicts (same dentist, overlapping time)
            var hasConflict = await _context.Appointments
                .AnyAsync(a => 
                    a.DentistId == appointment.DentistId &&
                    a.AppointmentDate == appointment.AppointmentDate &&
                    a.Status != "Cancelled" &&
                    ((a.StartTime <= appointment.StartTime && a.EndTime > appointment.StartTime) ||
                     (a.StartTime < appointment.EndTime && a.EndTime >= appointment.EndTime))
                );
            
            if (hasConflict)
            {
                ModelState.AddModelError("", "This time slot is already booked. Please choose another time.");
                
                // Reload dropdowns
                ViewBag.Services = new SelectList(
                    await _context.DentalServices.Where(s => s.IsActive).ToListAsync(), 
                    "Id", "Name");
                
                var dentists = await _userManager.GetUsersInRoleAsync("Dentist");
                ViewBag.Dentists = new SelectList(
                    dentists.Select(d => new { 
                        Id = d.Id, 
                        Name = $"Dr. {d.FirstName} {d.LastName}" 
                    }), 
                    "Id", "Name");
                
                return View(appointment);
            }
            
            _context.Add(appointment);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Cancel/5
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Dentist)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: Appointments/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = "Cancelled";
                appointment.UpdatedAt = DateTime.UtcNow;
                _context.Update(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}