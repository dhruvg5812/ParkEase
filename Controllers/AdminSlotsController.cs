using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.Models;

namespace ParkEase.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSlotsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminSlotsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? locationId, string? status, string? vehicleType)
        {
            var query = _context.ParkingSlots
                .Include(s => s.ParkingLocation)
                .AsQueryable();

            if (locationId.HasValue && locationId.Value > 0)
            {
                query = query.Where(s => s.ParkingLocationId == locationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(vehicleType))
            {
                query = query.Where(s => s.VehicleType == vehicleType);
            }

            var slots = await query
                .OrderBy(s => s.ParkingLocation!.Name)
                .ThenBy(s => s.SlotNumber)
                .ToListAsync();

            ViewBag.Locations = await _context.ParkingLocations
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name, Selected = locationId.HasValue && l.Id == locationId.Value })
                .ToListAsync();

            ViewBag.SelectedLocation = locationId;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedVehicleType = vehicleType;

            return View(slots);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Locations = await _context.ParkingLocations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .ToListAsync();

            return View(new ParkingSlot { HourlyRate = 40.00m, Status = "Available" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ParkingSlot slot)
        {
            // Validate unique slot number within location
            bool exists = await _context.ParkingSlots
                .AnyAsync(s => s.ParkingLocationId == slot.ParkingLocationId && s.SlotNumber.ToLower() == slot.SlotNumber.Trim().ToLower());

            if (exists)
            {
                ModelState.AddModelError("SlotNumber", "A slot with this number already exists in the selected location.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Locations = await _context.ParkingLocations
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.Name)
                    .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                    .ToListAsync();
                return View(slot);
            }

            slot.SlotNumber = slot.SlotNumber.Trim().ToUpper();
            slot.CreatedAt = DateTime.UtcNow;

            _context.ParkingSlots.Add(slot);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Slot '{slot.SlotNumber}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var slot = await _context.ParkingSlots.FindAsync(id);
            if (slot == null)
            {
                return NotFound();
            }

            ViewBag.Locations = await _context.ParkingLocations
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name, Selected = l.Id == slot.ParkingLocationId })
                .ToListAsync();

            return View(slot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ParkingSlot model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            bool exists = await _context.ParkingSlots
                .AnyAsync(s => s.Id != id && s.ParkingLocationId == model.ParkingLocationId && s.SlotNumber.ToLower() == model.SlotNumber.Trim().ToLower());

            if (exists)
            {
                ModelState.AddModelError("SlotNumber", "Another slot with this number already exists in this location.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Locations = await _context.ParkingLocations
                    .OrderBy(l => l.Name)
                    .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name, Selected = l.Id == model.ParkingLocationId })
                    .ToListAsync();
                return View(model);
            }

            var slot = await _context.ParkingSlots.FindAsync(id);
            if (slot == null)
            {
                return NotFound();
            }

            slot.SlotNumber = model.SlotNumber.Trim().ToUpper();
            slot.ParkingLocationId = model.ParkingLocationId;
            slot.VehicleType = model.VehicleType;
            slot.HourlyRate = model.HourlyRate;
            slot.Description = model.Description;
            slot.Status = model.Status;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Slot '{slot.SlotNumber}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string newStatus)
        {
            var slot = await _context.ParkingSlots.FindAsync(id);
            if (slot != null)
            {
                string[] validStatuses = { "Available", "Unavailable", "Maintenance" };
                if (validStatuses.Contains(newStatus))
                {
                    slot.Status = newStatus;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Slot '{slot.SlotNumber}' status changed to '{newStatus}'.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var slot = await _context.ParkingSlots.FindAsync(id);
            if (slot == null)
            {
                return NotFound();
            }

            bool hasBookings = await _context.ParkingBookings.AnyAsync(b => b.ParkingSlotId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = $"Cannot delete slot '{slot.SlotNumber}' because it has booking history. You can mark it as 'Unavailable' or 'Maintenance' instead.";
                return RedirectToAction(nameof(Index));
            }

            _context.ParkingSlots.Remove(slot);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Slot '{slot.SlotNumber}' was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
