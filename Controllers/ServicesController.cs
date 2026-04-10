using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToothSlot.Data;
using ToothSlot.Models;

namespace ToothSlot.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Services
        public async Task<IActionResult> Index()
        {
            // ✅ Show ALL services (no filter)
            var services = await _context.DentalServices
                .OrderBy(s => s.Name)
                .ToListAsync();
            
            return View(services);
        }

        // GET: Services/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DentalService service)
        {
            if (ModelState.IsValid)
            {
                service.IsActive = true;
                service.CreatedAt = DateTime.UtcNow;
                
                _context.Add(service);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Service created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // GET: Services/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.DentalServices.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        // POST: Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DentalService service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    service.UpdatedAt = DateTime.UtcNow;
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Service updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // POST: Services/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var service = await _context.DentalServices.FindAsync(id);
            
            if (service == null)
            {
                return NotFound();
            }

            service.IsActive = !service.IsActive;
            service.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Service '{service.Name}' {(service.IsActive ? "activated" : "deactivated")} successfully.";
            
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.DentalServices.Any(e => e.Id == id);
        }
    }
}