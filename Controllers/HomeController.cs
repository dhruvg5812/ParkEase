using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.Models;
using ParkEase.ViewModels;
using System.Diagnostics;

namespace ParkEase.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var locations = await _context.ParkingLocations
                .Where(l => l.IsActive)
                .Include(l => l.ParkingSlots)
                .ToListAsync();

            ViewBag.TotalLocations = locations.Count;
            ViewBag.TotalSlots = await _context.ParkingSlots.CountAsync();
            ViewBag.AvailableSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Available");
            ViewBag.FeaturedLocations = locations.Take(4).ToList();

            var searchModel = new ParkingSearchViewModel
            {
                SearchDate = DateTime.Today,
                StartTime = new TimeSpan(DateTime.Now.Hour + 1, 0, 0),
                EndTime = new TimeSpan(DateTime.Now.Hour + 3, 0, 0),
                Locations = locations.Select(l => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Name
                }).ToList()
            };

            return View(searchModel);
        }

        public IActionResult About()
        {
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
}
