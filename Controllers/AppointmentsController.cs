using Microsoft.AspNetCore.Authorization;
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

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Appointments
        [Authorize(Roles = "Patient,Dentist")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            List<Appointment> appointments;
            
            if (User.IsInRole("Dentist"))
            {
                appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Service)
                    .Where(a => a.DentistId == userId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.StartTime)
                    .ToListAsync();
            }
            else
            {
                appointments = await _context.Appointments
                    .Include(a => a.Dentist)
                    .Include(a => a.Service)
                    .Where(a => a.PatientId == userId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
            }
            
            return View(appointments);
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
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int serviceId, string dentistId, DateTime appointmentDate, TimeSpan startTime, string? notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var service = await _context.DentalServices.FindAsync(serviceId);
            if (service == null)
            {
                TempData["Error"] = "Invalid service selected.";
                return RedirectToAction(nameof(Book));
            }

            var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

            var conflictingAppointment = await _context.Appointments
                .Where(a => a.DentistId == dentistId &&
                            a.AppointmentDate.Date == appointmentDate.Date &&
                            a.Status != "Cancelled" &&
                            ((a.StartTime < endTime && a.EndTime > startTime)))
                .FirstOrDefaultAsync();

            if (conflictingAppointment != null)
            {
                TempData["Error"] = "This time slot is already booked. Please select a different time.";
                return RedirectToAction(nameof(Book));
            }

            var appointment = new Appointment
            {
                PatientId = userId,
                DentistId = dentistId,
                ServiceId = serviceId,
                AppointmentDate = appointmentDate,
                StartTime = startTime,
                EndTime = endTime,
                Status = "Pending",
                Notes = notes,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment booked successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Cancel/5
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Dentist)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (appointment.PatientId != userId)
            {
                return Forbid();
            }

            return View(appointment);
        }

        // POST: Appointments/CancelConfirmed
        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (appointment.PatientId != userId)
            {
                return Forbid();
            }

            appointment.Status = "Cancelled";
            appointment.UpdatedAt = DateTime.Now;

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}