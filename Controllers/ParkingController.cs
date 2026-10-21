using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.Services;
using ParkEase.ViewModels;

namespace ParkEase.Controllers
{
    public class ParkingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IParkingAvailabilityService _availabilityService;

        public ParkingController(
            ApplicationDbContext context,
            IParkingAvailabilityService availabilityService)
        {
            _context = context;
            _availabilityService = availabilityService;
        }

        public async Task<IActionResult> Index(
            int? locationId,
            string? vehicleType,
            DateTime? searchDate,
            TimeSpan? startTime,
            TimeSpan? endTime)
        {
            var locations = await _context.ParkingLocations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            DateTime date = searchDate ?? DateTime.Today;
            TimeSpan start = startTime ?? new TimeSpan(DateTime.Now.Hour + 1, 0, 0);
            TimeSpan end = endTime ?? new TimeSpan(DateTime.Now.Hour + 3, 0, 0);

            DateTime startDateTime = date.Date.Add(start);
            DateTime endDateTime = date.Date.Add(end);

            if (endDateTime <= startDateTime)
            {
                endDateTime = startDateTime.AddHours(2);
            }

            var slots = await _availabilityService.SearchSlotsAsync(locationId, vehicleType, startDateTime, endDateTime);

            var model = new ParkingSearchViewModel
            {
                LocationId = locationId,
                VehicleType = vehicleType,
                SearchDate = date,
                StartTime = start,
                EndTime = end,
                Locations = locations.Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Name,
                    Selected = locationId.HasValue && l.Id == locationId.Value
                }).ToList(),
                Slots = slots
            };

            // Store search criteria in session to reuse in booking form if user proceeds
            HttpContext.Session.SetString("Search_StartDateTime", startDateTime.ToString("o"));
            HttpContext.Session.SetString("Search_EndDateTime", endDateTime.ToString("o"));
            if (locationId.HasValue)
            {
                HttpContext.Session.SetInt32("Search_LocationId", locationId.Value);
            }

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var slot = await _context.ParkingSlots
                .Include(s => s.ParkingLocation)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null)
            {
                return NotFound();
            }

            var now = DateTime.Now;
            bool isAvailableNow = await _availabilityService.IsSlotAvailableAsync(slot.Id, now, now.AddHours(1));

            ViewBag.IsAvailableNow = isAvailableNow;
            return View(slot);
        }
    }
}
