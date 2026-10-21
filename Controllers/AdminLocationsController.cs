using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.Models;

namespace ParkEase.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLocationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminLocationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var locations = await _context.ParkingLocations
                .Include(l => l.ParkingSlots)
                .OrderBy(l => l.Name)
                .ToListAsync();

            return View(locations);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ParkingLocation());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ParkingLocation location)
        {
            if (!ModelState.IsValid)
            {
                return View(location);
            }

            location.CreatedAt = DateTime.UtcNow;
            _context.ParkingLocations.Add(location);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Location '{location.Name}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var location = await _context.ParkingLocations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }
            return View(location);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ParkingLocation model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var location = await _context.ParkingLocations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }

            location.Name = model.Name;
            location.Address = model.Address;
            location.Description = model.Description;
            location.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Location '{location.Name}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var location = await _context.ParkingLocations.FindAsync(id);
            if (location != null)
            {
                location.IsActive = !location.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Location '{location.Name}' is now {(location.IsActive ? "Active" : "Inactive")}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var location = await _context.ParkingLocations
                .Include(l => l.ParkingSlots)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                return NotFound();
            }

            var slotIds = location.ParkingSlots.Select(s => s.Id).ToList();
            bool hasBookings = await _context.ParkingBookings.AnyAsync(b => slotIds.Contains(b.ParkingSlotId));

            if (hasBookings)
            {
                TempData["ErrorMessage"] = $"Cannot delete location '{location.Name}' because it has historical or active bookings. You may deactivate it instead.";
                return RedirectToAction(nameof(Index));
            }

            _context.ParkingLocations.Remove(location);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Location '{location.Name}' was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
