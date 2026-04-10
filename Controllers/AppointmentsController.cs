using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (User.IsInRole("Patient"))
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Service)
                    .Include(a => a.Dentist)
                    .Where(a => a.PatientId == userId && a.Status != "Cancelled")
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.StartTime)
                    .ToListAsync();
                
                return View(appointments);
            }
            else if (User.IsInRole("Dentist"))
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Service)
                    .Include(a => a.Patient)
                    .Where(a => a.DentistId == userId && a.Status != "Cancelled")
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
            ViewBag.Services = await _context.DentalServices
                .Where(s => s.IsActive)
                .ToListAsync();
            
            ViewBag.Dentists = await _context.DentistProfiles
                .Include(d => d.User)
                .Where(d => d.IsActive)
                .ToListAsync();
            
            return View();
        }

        // POST: Appointments/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Book(int serviceId, string dentistId, DateTime appointmentDate, string appointmentTime, string? notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Parse the time string to TimeSpan
            if (!TimeSpan.TryParse(appointmentTime, out TimeSpan startTime))
            {
                ModelState.AddModelError("", "Invalid time format.");
                
                ViewBag.Services = await _context.DentalServices.Where(s => s.IsActive).ToListAsync();
                ViewBag.Dentists = await _context.DentistProfiles.Include(d => d.User).Where(d => d.IsActive).ToListAsync();
                
                return View();
            }

            // Check for conflicts
            var conflict = await _context.Appointments
                .AnyAsync(a => a.DentistId == dentistId
                           && a.AppointmentDate == appointmentDate
                           && a.StartTime == startTime
                           && a.Status != "Cancelled");
            
            if (conflict)
            {
                ModelState.AddModelError("", "This time slot is already booked. Please choose a different time or dentist.");
                
                ViewBag.Services = await _context.DentalServices.Where(s => s.IsActive).ToListAsync();
                ViewBag.Dentists = await _context.DentistProfiles.Include(d => d.User).Where(d => d.IsActive).ToListAsync();
                
                return View();
            }

            // ✅ NEW: Check dentist availability
            var isAvailable = await IsDentistAvailable(dentistId, appointmentDate, startTime);
            if (!isAvailable)
            {
                ModelState.AddModelError("", "The selected dentist is not available at this time. Please choose a different time or date.");
                
                ViewBag.Services = await _context.DentalServices.Where(s => s.IsActive).ToListAsync();
                ViewBag.Dentists = await _context.DentistProfiles.Include(d => d.User).Where(d => d.IsActive).ToListAsync();
                
                return View();
            }

            // Get service to calculate end time
            var service = await _context.DentalServices.FindAsync(serviceId);
            if (service == null)
            {
                ModelState.AddModelError("", "Invalid service selected.");
                
                ViewBag.Services = await _context.DentalServices.Where(s => s.IsActive).ToListAsync();
                ViewBag.Dentists = await _context.DentistProfiles.Include(d => d.User).Where(d => d.IsActive).ToListAsync();
                
                return View();
            }

            var appointment = new Appointment
            {
                PatientId = userId,
                DentistId = dentistId,
                ServiceId = serviceId,
                AppointmentDate = appointmentDate,
                StartTime = startTime,
                EndTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes)),
                Status = "Pending",
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Appointment booked successfully for {appointmentDate:MMM dd, yyyy} at {startTime:hh\\:mm}!";
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Reschedule/5
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Reschedule(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Dentist)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Verify patient owns this appointment
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.PatientId != userId)
            {
                return Forbid();
            }

            // Can't reschedule past appointments
            if (appointment.AppointmentDate < DateTime.Now.Date)
            {
                TempData["Error"] = "Cannot reschedule past appointments.";
                return RedirectToAction(nameof(Index));
            }

            // Can't reschedule cancelled appointments
            if (appointment.Status == "Cancelled")
            {
                TempData["Error"] = "Cannot reschedule cancelled appointments.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Services = await _context.DentalServices
                .Where(s => s.IsActive)
                .ToListAsync();
            
            ViewBag.Dentists = await _context.DentistProfiles
                .Include(d => d.User)
                .Where(d => d.IsActive)
                .ToListAsync();

            return View(appointment);
        }

        // POST: Appointments/Reschedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Reschedule(int id, string dentistId, DateTime appointmentDate, string appointmentTime)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Verify patient owns this appointment
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.PatientId != userId)
            {
                return Forbid();
            }

            // Parse the time string to TimeSpan
            if (!TimeSpan.TryParse(appointmentTime, out TimeSpan startTime))
            {
                ModelState.AddModelError("", "Invalid time format.");
                
                ViewBag.Services = await _context.DentalServices.Where(s => s.IsActive).ToListAsync();
                ViewBag.Dentists = await _context.DentistProfiles.Include(d => d.User).Where(d => d.IsActive).ToListAsync();
                
                return View(appointment);
            }

            // Check for conflicts (excluding current appointment)
            var conflict = await _context.Appointments
                .AnyAsync(a => a.Id != id
                           && a.DentistId == dentistId
                           && a.AppointmentDate == appointmentDate
                           && a.StartTime == startTime
                           && a.Status != "Cancelled");
            
            if (conflict)
            {
                ModelState.AddModelError("", "This time slot is already booked. Please choose a different time or dentist.");
                
                ViewBag.Services = await _context.DentalServices.Where(s => s.IsActive).ToListAsync();
                ViewBag.Dentists = await _context.DentistProfiles.Include(d => d.User).Where(d => d.IsActive).ToListAsync();
                
                return View(appointment);
            }

            // ✅ NEW: Check dentist availability
            var isAvailable = await IsDentistAvailable(dentistId, appointmentDate, startTime);
            if (!isAvailable)
            {
                ModelState.AddModelError("", "The selected dentist is not available at this time. Please choose a different time or date.");
                
                ViewBag.Services = await _context.DentalServices.Where(s => s.IsActive).ToListAsync();
                ViewBag.Dentists = await _context.DentistProfiles.Include(d => d.User).Where(d => d.IsActive).ToListAsync();
                
                return View(appointment);
            }

            // Get service to calculate end time
            var service = await _context.DentalServices.FindAsync(appointment.ServiceId);
            if (service == null)
            {
                ModelState.AddModelError("", "Service not found.");
                
                ViewBag.Services = await _context.DentalServices.Where(s => s.IsActive).ToListAsync();
                ViewBag.Dentists = await _context.DentistProfiles.Include(d => d.User).Where(d => d.IsActive).ToListAsync();
                
                return View(appointment);
            }

            // Update appointment
            appointment.DentistId = dentistId;
            appointment.AppointmentDate = appointmentDate;
            appointment.StartTime = startTime;
            appointment.EndTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Appointment rescheduled successfully to {appointmentDate:MMM dd, yyyy} at {startTime:hh\\:mm}!";
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Appointments/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            
            if (appointment == null)
            {
                return NotFound();
            }
            
            if (appointment.AppointmentDate < DateTime.Now.Date)
            {
                TempData["Error"] = "Cannot cancel appointments in the past.";
                return RedirectToAction(nameof(Index));
            }
            
            // Verify patient owns this appointment
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.PatientId != userId)
            {
                return Forbid();
            }
            
            appointment.Status = "Cancelled";
            appointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Appointment cancelled successfully.";
            
            return RedirectToAction(nameof(Index));
        }

        // ✅ NEW: Helper method to check if dentist is available at the given time
        private async Task<bool> IsDentistAvailable(string dentistId, DateTime appointmentDate, TimeSpan appointmentTime)
        {
            var dayOfWeek = (int)appointmentDate.DayOfWeek;
            
            var availability = await _context.DentistAvailabilities
                .Where(a => a.DentistId == dentistId 
                         && a.DayOfWeek == dayOfWeek
                         && a.IsAvailable
                         && a.StartTime <= appointmentTime
                         && a.EndTime > appointmentTime)
                .AnyAsync();
            
            return availability;
        }
    }
}