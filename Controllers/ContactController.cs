using Microsoft.AspNetCore.Mvc;
using ParkEase.Data;
using ParkEase.Models;
using ParkEase.ViewModels;

namespace ParkEase.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ContactViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var message = new ContactMessage
            {
                Name = model.Name.Trim(),
                Email = model.Email.Trim(),
                Subject = model.Subject.Trim(),
                Message = model.Message.Trim(),
                SubmittedAt = DateTime.UtcNow,
                Status = "New"
            };

            _context.ContactMessages.Add(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you for contacting ParkEase! Your inquiry has been submitted successfully. Our team will get back to you shortly.";
            return RedirectToAction(nameof(Index));
        }
    }
}
