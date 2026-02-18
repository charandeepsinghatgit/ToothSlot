using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToothSlot.Data;
using ToothSlot.Models;

namespace ToothSlot.Controllers
{
    [Authorize(Roles = "Dentist")]
    public class DentistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DentistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Dentist/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var today = DateTime.Today;
            
            // Get today's appointments
            var todayAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Service)
                .Where(a => a.DentistId == user.Id && 
                           a.AppointmentDate == today &&
                           a.Status != "Cancelled")
                .OrderBy(a => a.StartTime)
                .ToListAsync();
            
            ViewBag.TodayDate = today.ToString("MMMM dd, yyyy");
            ViewBag.TotalAppointments = todayAppointments.Count;
            
            return View(todayAppointments);
        }

        // GET: Dentist/Schedule?date=2026-02-17
        public async Task<IActionResult> Schedule(DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            var selectedDate = date ?? DateTime.Today;
            
            // Get appointments for selected date
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Service)
                .Where(a => a.DentistId == user.Id && 
                           a.AppointmentDate == selectedDate)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
            
            ViewBag.SelectedDate = selectedDate;
            ViewBag.FormattedDate = selectedDate.ToString("MMMM dd, yyyy");
            
            return View(appointments);
        }

        // GET: Dentist/Upcoming
        public async Task<IActionResult> Upcoming()
        {
            var user = await _userManager.GetUserAsync(User);
            var today = DateTime.Today;
            
            // Get upcoming appointments (next 7 days)
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Service)
                .Where(a => a.DentistId == user.Id && 
                           a.AppointmentDate >= today &&
                           a.AppointmentDate <= today.AddDays(7) &&
                           a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync();
            
            return View(upcomingAppointments);
        }

        // POST: Dentist/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            
            if (appointment == null)
            {
                return NotFound();
            }
            
            // Verify this dentist owns this appointment
            var user = await _userManager.GetUserAsync(User);
            if (appointment.DentistId != user.Id)
            {
                return Forbid();
            }
            
            appointment.Status = status;
            appointment.UpdatedAt = DateTime.UtcNow;
            
            _context.Update(appointment);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Dashboard));
        }
    }
}