using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ToothSlot.Data;

namespace ToothSlot.Controllers
{
    [Authorize(Roles = "Dentist")]
    public class DentistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DentistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dashboard - Shows TODAY's appointments for logged-in dentist
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Get ALL appointments for today (Pending, Confirmed, Completed - NOT Cancelled)
            var todaysAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Service)
                .Where(a => a.DentistId == userId && 
                            a.AppointmentDate.Date == DateTime.Today &&
                            a.Status != "Cancelled")
                .OrderBy(a => a.StartTime)
                .ToListAsync();
            
            ViewBag.TodaysDate = DateTime.Today.ToString("MMMM dd, yyyy");
            ViewBag.AppointmentCount = todaysAppointments.Count;
            
            return View(todaysAppointments);
        }

        // Upcoming - Shows next 7 days of appointments
        public async Task<IActionResult> Upcoming()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Service)
                .Where(a => a.DentistId == userId && 
                            a.AppointmentDate >= DateTime.Today &&
                            a.AppointmentDate <= DateTime.Today.AddDays(7) &&
                            a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync();
            
            ViewBag.StartDate = DateTime.Today.ToString("MMMM dd");
            ViewBag.EndDate = DateTime.Today.AddDays(7).ToString("MMMM dd, yyyy");
            
            return View(upcomingAppointments);
        }
    }
}
