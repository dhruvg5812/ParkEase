using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.Models;
using ParkEase.Services;
using ParkEase.ViewModels;

namespace ParkEase.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBookingService _bookingService;
        private readonly IFeeCalculationService _feeCalculationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsController(
            ApplicationDbContext context,
            IBookingService bookingService,
            IFeeCalculationService feeCalculationService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _bookingService = bookingService;
            _feeCalculationService = feeCalculationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int slotId, DateTime? start = null, DateTime? end = null)
        {
            var slot = await _context.ParkingSlots
                .Include(s => s.ParkingLocation)
                .FirstOrDefaultAsync(s => s.Id == slotId);

            if (slot == null)
            {
                TempData["ErrorMessage"] = "The selected parking slot could not be found.";
                return RedirectToAction("Index", "Parking");
            }

            if (slot.Status != "Available")
            {
                TempData["ErrorMessage"] = $"This parking slot is currently {slot.Status} and cannot be reserved.";
                return RedirectToAction("Index", "Parking");
            }

            // Check session for preselected search dates
            DateTime startTime = start ?? DateTime.Now.AddMinutes(15);
            DateTime endTime = end ?? DateTime.Now.AddHours(2).AddMinutes(15);

            if (start == null && HttpContext.Session.GetString("Search_StartDateTime") != null)
            {
                if (DateTime.TryParse(HttpContext.Session.GetString("Search_StartDateTime"), out var sessionStart))
                {
                    startTime = sessionStart;
                }
            }

            if (end == null && HttpContext.Session.GetString("Search_EndDateTime") != null)
            {
                if (DateTime.TryParse(HttpContext.Session.GetString("Search_EndDateTime"), out var sessionEnd))
                {
                    endTime = sessionEnd;
                }
            }

            if (endTime <= startTime)
            {
                endTime = startTime.AddHours(2);
            }

            var (billableHours, estimatedFee) = _feeCalculationService.CalculateEstimatedFee(slot.HourlyRate, startTime, endTime);

            var model = new BookingCreateViewModel
            {
                ParkingSlotId = slot.Id,
                SlotNumber = slot.SlotNumber,
                ParkingLocationId = slot.ParkingLocationId,
                LocationName = slot.ParkingLocation?.Name ?? "ParkEase Hub",
                LocationAddress = slot.ParkingLocation?.Address ?? "",
                VehicleType = slot.VehicleType,
                HourlyRate = slot.HourlyRate,
                StartDateTime = startTime,
                EndDateTime = endTime,
                BillableHours = billableHours,
                EstimatedFee = estimatedFee
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var slot = await _context.ParkingSlots
                .Include(s => s.ParkingLocation)
                .FirstOrDefaultAsync(s => s.Id == model.ParkingSlotId);

            if (slot == null)
            {
                ModelState.AddModelError(string.Empty, "The selected slot does not exist.");
                return View(model);
            }

            // Fill non-submitted display fields
            model.SlotNumber = slot.SlotNumber;
            model.LocationName = slot.ParkingLocation?.Name ?? "ParkEase Hub";
            model.LocationAddress = slot.ParkingLocation?.Address ?? "";
            model.VehicleType = slot.VehicleType;
            model.HourlyRate = slot.HourlyRate;

            var (billable, fee) = _feeCalculationService.CalculateEstimatedFee(slot.HourlyRate, model.StartDateTime, model.EndDateTime);
            model.BillableHours = billable;
            model.EstimatedFee = fee;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, message, booking) = await _bookingService.CreateBookingAsync(model, user.Id);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Reservation successfully created!";
            return RedirectToAction("Confirmation", new { id = booking!.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var booking = await _bookingService.GetBookingDetailsAsync(id, user.Id, User.IsInRole("Admin"));
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking details not found or unauthorized access.";
                return RedirectToAction("MyBookings");
            }

            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings(string? status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var bookings = await _bookingService.GetUserBookingsAsync(user.Id, status);

            var model = new MyBookingsViewModel
            {
                FilterStatus = status,
                Bookings = bookings
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var booking = await _bookingService.GetBookingDetailsAsync(id, user.Id, User.IsInRole("Admin"));
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found or you do not have permission to view it.";
                return RedirectToAction("MyBookings");
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var (success, message) = await _bookingService.CancelBookingAsync(id, user.Id, User.IsInRole("Admin"));

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction("MyBookings");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult CalculateFeeJson(decimal rate, DateTime start, DateTime end)
        {
            var (hours, fee) = _feeCalculationService.CalculateEstimatedFee(rate, start, end);
            return Json(new { billableHours = hours, estimatedFee = fee });
        }
    }
}
