using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.ViewModels;

namespace ParkEase.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var totalSlots = await _context.ParkingSlots.CountAsync();
            var availableSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Available");
            var maintenanceSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Maintenance");
            var activeCheckIns = await _context.ParkingSessions.CountAsync(s => s.Status == "CheckedIn");

            var todayReservations = await _context.ParkingBookings
                .CountAsync(b => b.StartDateTime >= today && b.StartDateTime < tomorrow && b.BookingStatus != "Cancelled");

            var upcomingReservations = await _context.ParkingBookings
                .CountAsync(b => b.StartDateTime >= now && b.BookingStatus == "Confirmed");

            var completedBookings = await _context.ParkingBookings
                .CountAsync(b => b.BookingStatus == "Completed");

            // Revenue calculation: Sum of finalized sessions or completed bookings
            var completedRevenue = await _context.ParkingSessions
                .Where(s => s.Status == "CheckedOut" && s.FinalFee.HasValue)
                .SumAsync(s => s.FinalFee!.Value);

            var activeEstimatedRevenue = await _context.ParkingBookings
                .Where(b => b.BookingStatus == "Confirmed")
                .SumAsync(b => b.EstimatedFee);

            // Recent Bookings (top 8)
            var recentBookings = await _context.ParkingBookings
                .Include(b => b.User)
                .Include(b => b.ParkingSlot)
                    .ThenInclude(s => s!.ParkingLocation)
                .Include(b => b.Session)
                .OrderByDescending(b => b.CreatedAt)
                .Take(8)
                .ToListAsync();

            // Location-wise statistics
            var locations = await _context.ParkingLocations
                .Include(l => l.ParkingSlots)
                .ToListAsync();

            var locationStats = new List<LocationSlotStat>();
            foreach (var loc in locations)
            {
                var locSlotIds = loc.ParkingSlots.Select(s => s.Id).ToList();
                var locBookings = await _context.ParkingBookings
                    .Where(b => locSlotIds.Contains(b.ParkingSlotId))
                    .ToListAsync();

                var locRevenue = await _context.ParkingSessions
                    .Where(s => s.Status == "CheckedOut" && s.FinalFee.HasValue && locSlotIds.Contains(s.Booking!.ParkingSlotId))
                    .SumAsync(s => s.FinalFee!.Value);

                locationStats.Add(new LocationSlotStat
                {
                    LocationId = loc.Id,
                    LocationName = loc.Name,
                    TotalSlots = loc.ParkingSlots.Count,
                    AvailableSlots = loc.ParkingSlots.Count(s => s.Status == "Available"),
                    ActiveBookings = locBookings.Count(b => b.BookingStatus == "Confirmed"),
                    Revenue = locRevenue
                });
            }

            var recentMessages = await _context.ContactMessages
                .OrderByDescending(m => m.SubmittedAt)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalParkingSlots = totalSlots,
                AvailableSlots = availableSlots,
                OccupiedSlots = activeCheckIns,
                MaintenanceSlots = maintenanceSlots,
                TodayReservations = todayReservations,
                UpcomingReservations = upcomingReservations,
                ActiveCheckIns = activeCheckIns,
                CompletedBookings = completedBookings,
                TotalRecordedRevenue = completedRevenue,
                EstimatedActiveRevenue = activeEstimatedRevenue,
                RecentBookings = recentBookings,
                LocationStats = locationStats,
                RecentMessages = recentMessages
            };

            return View(model);
        }
    }
}
