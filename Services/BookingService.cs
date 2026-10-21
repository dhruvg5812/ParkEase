using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.Models;
using ParkEase.ViewModels;

namespace ParkEase.Services
{
    public interface IBookingService
    {
        Task<(bool Success, string Message, ParkingBooking? Booking)> CreateBookingAsync(BookingCreateViewModel model, string userId);
        Task<(bool Success, string Message)> CancelBookingAsync(int bookingId, string userId, bool isAdmin = false);
        Task<(bool Success, string Message, ParkingSession? Session)> CheckInAsync(int bookingId);
        Task<(bool Success, string Message, ParkingSession? Session)> CheckOutAsync(int bookingId);
        Task<BookingDetailsViewModel?> GetBookingDetailsAsync(int bookingId, string? userId = null, bool isAdmin = false);
        Task<List<ParkingBooking>> GetUserBookingsAsync(string userId, string? statusFilter = null);
        Task<List<ParkingBooking>> GetAllBookingsAsync(string? search = null, string? status = null, DateTime? date = null);
    }

    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IParkingAvailabilityService _availabilityService;
        private readonly IFeeCalculationService _feeCalculationService;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            ApplicationDbContext context,
            IParkingAvailabilityService availabilityService,
            IFeeCalculationService feeCalculationService,
            ILogger<BookingService> logger)
        {
            _context = context;
            _availabilityService = availabilityService;
            _feeCalculationService = feeCalculationService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, ParkingBooking? Booking)> CreateBookingAsync(BookingCreateViewModel model, string userId)
        {
            // Basic parameter validations
            if (model.EndDateTime <= model.StartDateTime)
            {
                return (false, "End date and time must be later than start date and time.", null);
            }

            if (model.StartDateTime < DateTime.Now.AddMinutes(-10))
            {
                return (false, "Reservations cannot be made in the past.", null);
            }

            // Begin execution strategy and transaction to handle concurrency safely
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<(bool Success, string Message, ParkingBooking? Booking)>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                try
                {
                    var slot = await _context.ParkingSlots
                        .Include(s => s.ParkingLocation)
                        .FirstOrDefaultAsync(s => s.Id == model.ParkingSlotId);

                    if (slot == null)
                    {
                        return (false, "The selected parking slot does not exist.", null);
                    }

                    if (slot.Status != "Available")
                    {
                        return (false, $"This slot is currently marked as {slot.Status} and cannot be reserved.", null);
                    }

                    if (slot.ParkingLocation == null || !slot.ParkingLocation.IsActive)
                    {
                        return (false, "The parking location is currently inactive.", null);
                    }

                    // Check conflict on the server within transaction
                    bool hasConflict = await _availabilityService.HasConflictAsync(slot.Id, model.StartDateTime, model.EndDateTime);
                    if (hasConflict)
                    {
                        return (false, "This slot has already been reserved for the selected time range. Please choose a different slot or time.", null);
                    }

                    // Calculate fee strictly on the server using slot's current rate
                    var (billableHours, estimatedFee) = _feeCalculationService.CalculateEstimatedFee(
                        slot.HourlyRate, model.StartDateTime, model.EndDateTime);

                    // Generate a collision-resistant unique booking reference
                    string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
                    string randPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
                    string bookingRef = $"PK-{datePart}-{randPart}";

                    var booking = new ParkingBooking
                    {
                        BookingReference = bookingRef,
                        UserId = userId,
                        ParkingSlotId = slot.Id,
                        VehicleNumber = model.VehicleNumber.Trim().ToUpper(),
                        VehicleType = slot.VehicleType,
                        StartDateTime = model.StartDateTime,
                        EndDateTime = model.EndDateTime,
                        HourlyRateAtBooking = slot.HourlyRate,
                        EstimatedFee = estimatedFee,
                        BookingStatus = "Confirmed",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ParkingBookings.Add(booking);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Booking {Ref} created for slot {Slot} by user {User}", bookingRef, slot.SlotNumber, userId);

                    return (true, "Booking confirmed successfully!", booking);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error occurred while creating booking for slot {SlotId}", model.ParkingSlotId);
                    return (false, "An unexpected error occurred while processing your booking. Please try again.", null);
                }
            });
        }

        public async Task<(bool Success, string Message)> CancelBookingAsync(int bookingId, string userId, bool isAdmin = false)
        {
            var booking = await _context.ParkingBookings
                .Include(b => b.Session)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return (false, "Booking not found.");
            }

            // Ownership check
            if (!isAdmin && booking.UserId != userId)
            {
                return (false, "Unauthorized: You can only cancel your own bookings.");
            }

            // Check eligibility
            if (booking.BookingStatus == "Cancelled")
            {
                return (false, "This booking has already been cancelled.");
            }

            if (booking.BookingStatus == "Completed")
            {
                return (false, "Completed reservations cannot be cancelled.");
            }

            if (booking.Session != null)
            {
                return (false, "Cannot cancel a booking that has already been checked in.");
            }

            if (booking.EndDateTime < DateTime.Now)
            {
                return (false, "Cannot cancel past reservations.");
            }

            booking.BookingStatus = "Cancelled";
            booking.CancelledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Booking {Ref} cancelled by user {User} (IsAdmin: {IsAdmin})", booking.BookingReference, userId, isAdmin);

            return (true, "Reservation cancelled successfully. The slot is now available for other users.");
        }

        public async Task<(bool Success, string Message, ParkingSession? Session)> CheckInAsync(int bookingId)
        {
            var booking = await _context.ParkingBookings
                .Include(b => b.Session)
                .Include(b => b.ParkingSlot)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return (false, "Booking not found.", null);
            }

            if (booking.BookingStatus == "Cancelled")
            {
                return (false, "Cannot check in a cancelled booking.", null);
            }

            if (booking.BookingStatus == "Completed")
            {
                return (false, "This booking has already been completed.", null);
            }

            if (booking.Session != null)
            {
                return (false, "This vehicle has already been checked in.", booking.Session);
            }

            var session = new ParkingSession
            {
                BookingId = booking.Id,
                CheckInTime = DateTime.UtcNow,
                Status = "CheckedIn"
            };

            _context.ParkingSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Vehicle {Vehicle} checked in for booking {Ref}", booking.VehicleNumber, booking.BookingReference);

            return (true, "Vehicle checked in successfully.", session);
        }

        public async Task<(bool Success, string Message, ParkingSession? Session)> CheckOutAsync(int bookingId)
        {
            var booking = await _context.ParkingBookings
                .Include(b => b.Session)
                .Include(b => b.ParkingSlot)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return (false, "Booking not found.", null);
            }

            if (booking.Session == null)
            {
                return (false, "Vehicle cannot check out before checking in.", null);
            }

            if (booking.Session.Status == "CheckedOut")
            {
                return (false, "Vehicle has already been checked out.", booking.Session);
            }

            var checkOutTime = DateTime.UtcNow;
            booking.Session.CheckOutTime = checkOutTime;
            booking.Session.Status = "CheckedOut";

            int durationMinutes = (int)(checkOutTime - booking.Session.CheckInTime).TotalMinutes;
            booking.Session.ActualDurationMinutes = durationMinutes > 0 ? durationMinutes : 1;

            var (billableHours, finalFee) = _feeCalculationService.CalculateActualFee(
                booking.HourlyRateAtBooking, booking.Session.CheckInTime, checkOutTime);

            booking.Session.FinalFee = finalFee;
            booking.BookingStatus = "Completed";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Vehicle {Vehicle} checked out. Fee: {Fee}", booking.VehicleNumber, finalFee);

            return (true, $"Vehicle checked out successfully. Final Fee: ₹{finalFee:N2} ({billableHours} hr billable)", booking.Session);
        }

        public async Task<BookingDetailsViewModel?> GetBookingDetailsAsync(int bookingId, string? userId = null, bool isAdmin = false)
        {
            var query = _context.ParkingBookings
                .Include(b => b.User)
                .Include(b => b.ParkingSlot)
                    .ThenInclude(s => s!.ParkingLocation)
                .Include(b => b.Session)
                .Where(b => b.Id == bookingId);

            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(b => b.UserId == userId);
            }

            var booking = await query.FirstOrDefaultAsync();
            if (booking == null) return null;

            return new BookingDetailsViewModel
            {
                Id = booking.Id,
                BookingReference = booking.BookingReference,
                UserId = booking.UserId,
                CustomerName = booking.User?.FullName ?? "Customer",
                CustomerEmail = booking.User?.Email ?? "",
                CustomerPhone = booking.User?.PhoneNumber,
                SlotId = booking.ParkingSlotId,
                SlotNumber = booking.ParkingSlot?.SlotNumber ?? "N/A",
                LocationName = booking.ParkingSlot?.ParkingLocation?.Name ?? "ParkEase Hub",
                LocationAddress = booking.ParkingSlot?.ParkingLocation?.Address ?? "",
                VehicleNumber = booking.VehicleNumber,
                VehicleType = booking.VehicleType,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                HourlyRateAtBooking = booking.HourlyRateAtBooking,
                EstimatedFee = booking.EstimatedFee,
                BookingStatus = booking.BookingStatus,
                CreatedAt = booking.CreatedAt,
                CancelledAt = booking.CancelledAt,
                Session = booking.Session
            };
        }

        public async Task<List<ParkingBooking>> GetUserBookingsAsync(string userId, string? statusFilter = null)
        {
            var query = _context.ParkingBookings
                .Include(b => b.ParkingSlot)
                    .ThenInclude(s => s!.ParkingLocation)
                .Include(b => b.Session)
                .Where(b => b.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(b => b.BookingStatus == statusFilter);
            }

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ParkingBooking>> GetAllBookingsAsync(string? search = null, string? status = null, DateTime? date = null)
        {
            var query = _context.ParkingBookings
                .Include(b => b.User)
                .Include(b => b.ParkingSlot)
                    .ThenInclude(s => s!.ParkingLocation)
                .Include(b => b.Session)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(b =>
                    b.BookingReference.Contains(search) ||
                    b.VehicleNumber.Contains(search) ||
                    (b.User != null && b.User.FullName.Contains(search)) ||
                    (b.User != null && b.User.Email!.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.BookingStatus == status);
            }

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                var nextDate = targetDate.AddDays(1);
                query = query.Where(b => b.StartDateTime >= targetDate && b.StartDateTime < nextDate);
            }

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
    }
}
