using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.Models;
using ParkEase.ViewModels;

namespace ParkEase.Services
{
    public interface IParkingAvailabilityService
    {
        Task<bool> IsSlotAvailableAsync(int slotId, DateTime start, DateTime end, int? excludeBookingId = null);
        Task<List<ParkingSlotCardViewModel>> SearchSlotsAsync(int? locationId, string? vehicleType, DateTime? start, DateTime? end);
        Task<bool> HasConflictAsync(int slotId, DateTime start, DateTime end, int? excludeBookingId = null);
    }

    public class ParkingAvailabilityService : IParkingAvailabilityService
    {
        private readonly ApplicationDbContext _context;

        public ParkingAvailabilityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsSlotAvailableAsync(int slotId, DateTime start, DateTime end, int? excludeBookingId = null)
        {
            var slot = await _context.ParkingSlots
                .Include(s => s.ParkingLocation)
                .FirstOrDefaultAsync(s => s.Id == slotId);

            if (slot == null || slot.Status != "Available" || (slot.ParkingLocation != null && !slot.ParkingLocation.IsActive))
            {
                return false;
            }

            bool hasConflict = await HasConflictAsync(slotId, start, end, excludeBookingId);
            return !hasConflict;
        }

        public async Task<bool> HasConflictAsync(int slotId, DateTime start, DateTime end, int? excludeBookingId = null)
        {
            return await _context.ParkingBookings.AnyAsync(b =>
                b.ParkingSlotId == slotId &&
                (b.BookingStatus == "Confirmed" || b.BookingStatus == "Pending") &&
                b.StartDateTime < end &&
                b.EndDateTime > start &&
                (!excludeBookingId.HasValue || b.Id != excludeBookingId.Value));
        }

        public async Task<List<ParkingSlotCardViewModel>> SearchSlotsAsync(
            int? locationId,
            string? vehicleType,
            DateTime? start,
            DateTime? end)
        {
            var query = _context.ParkingSlots
                .Include(s => s.ParkingLocation)
                .Where(s => s.ParkingLocation!.IsActive)
                .AsQueryable();

            if (locationId.HasValue && locationId.Value > 0)
            {
                query = query.Where(s => s.ParkingLocationId == locationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(vehicleType))
            {
                query = query.Where(s => s.VehicleType == vehicleType);
            }

            var slots = await query.ToListAsync();
            var now = DateTime.UtcNow;

            DateTime checkStart = start ?? now;
            DateTime checkEnd = end ?? now.AddHours(2);

            var result = new List<ParkingSlotCardViewModel>();

            foreach (var slot in slots)
            {
                bool isAvailableForSearch = false;
                if (slot.Status == "Available")
                {
                    bool conflict = await HasConflictAsync(slot.Id, checkStart, checkEnd);
                    isAvailableForSearch = !conflict;
                }

                bool isAvailableNow = false;
                if (slot.Status == "Available")
                {
                    bool currentConflict = await HasConflictAsync(slot.Id, now, now.AddHours(1));
                    isAvailableNow = !currentConflict;
                }

                result.Add(new ParkingSlotCardViewModel
                {
                    Id = slot.Id,
                    SlotNumber = slot.SlotNumber,
                    LocationId = slot.ParkingLocationId,
                    LocationName = slot.ParkingLocation?.Name ?? "Main Campus",
                    LocationAddress = slot.ParkingLocation?.Address ?? "",
                    VehicleType = slot.VehicleType,
                    HourlyRate = slot.HourlyRate,
                    Description = slot.Description,
                    Status = slot.Status,
                    IsAvailableForSearch = isAvailableForSearch,
                    IsAvailableNow = isAvailableNow
                });
            }

            return result;
        }
    }
}
