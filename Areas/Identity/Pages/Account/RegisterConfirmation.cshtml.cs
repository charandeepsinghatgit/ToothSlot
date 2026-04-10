using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ToothSlot.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        public IActionResult OnGet(string email, string returnUrl = null)
        {
            // ✅ Auto-redirect new registrations to Appointments
            return RedirectToAction("Index", "Appointments");
        }
    }
}