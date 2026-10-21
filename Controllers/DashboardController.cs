using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.Models;
using ParkEase.ViewModels;

namespace ParkEase.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // If user is Admin, redirect to Admin Dashboard
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var allUserBookings = await _context.ParkingBookings
                .Include(b => b.ParkingSlot)
                    .ThenInclude(s => s!.ParkingLocation)
                .Include(b => b.Session)
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var now = DateTime.Now;

            var upcoming = allUserBookings
                .Where(b => b.BookingStatus == "Confirmed" && b.EndDateTime >= now)
                .OrderBy(b => b.StartDateTime)
                .ToList();

            var activeCheckIns = allUserBookings
                .Count(b => b.Session != null && b.Session.Status == "CheckedIn");

            var totalSpent = allUserBookings
                .Where(b => b.BookingStatus == "Completed" || b.BookingStatus == "Confirmed")
                .Sum(b => b.Session?.FinalFee ?? b.EstimatedFee);

            var model = new CustomerDashboardViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                TotalBookings = allUserBookings.Count,
                UpcomingBookingsCount = upcoming.Count,
                ActiveCheckInsCount = activeCheckIns,
                TotalSpent = totalSpent,
                UpcomingBookings = upcoming.Take(5).ToList(),
                RecentBookings = allUserBookings.Take(6).ToList()
            };

            return View(model);
        }
    }
}
