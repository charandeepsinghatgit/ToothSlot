using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ToothSlot.Models;

namespace ToothSlot.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Redirect authenticated users to their appropriate page
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Patient"))
            {
                return RedirectToAction("Index", "Appointments");
            }
            else if (User.IsInRole("Dentist"))
            {
                return RedirectToAction("Dashboard", "Dentist");
            }
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Services");  // ✅ FIXED
            }
        }
        
        // If not logged in, show the welcome page
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}