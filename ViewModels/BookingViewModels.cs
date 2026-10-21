using System.ComponentModel.DataAnnotations;
using ParkEase.Models;

namespace ParkEase.ViewModels
{
    public class BookingCreateViewModel : IValidatableObject
    {
        [Required]
        public int ParkingSlotId { get; set; }

        public string SlotNumber { get; set; } = string.Empty;
        public int ParkingLocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;
        public string VehicleType { get; set; } = "Car";
        public decimal HourlyRate { get; set; }

        [Required(ErrorMessage = "Vehicle registration number is required.")]
        [StringLength(30, ErrorMessage = "Vehicle number cannot exceed 30 characters.")]
        [Display(Name = "Vehicle Registration Number (e.g. MH12AB1234)")]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date and time is required.")]
        [Display(Name = "Reservation Start Time")]
        public DateTime StartDateTime { get; set; } = DateTime.Now.AddMinutes(15);

        [Required(ErrorMessage = "End date and time is required.")]
        [Display(Name = "Reservation End Time")]
        public DateTime EndDateTime { get; set; } = DateTime.Now.AddHours(2).AddMinutes(15);

        [Display(Name = "Billable Hours")]
        public int BillableHours { get; set; }

        [Display(Name = "Estimated Fee")]
        public decimal EstimatedFee { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDateTime <= StartDateTime)
            {
                yield return new ValidationResult(
                    "End date and time must be later than the start date and time.",
                    new[] { nameof(EndDateTime) });
            }

            if (StartDateTime < DateTime.Now.AddMinutes(-10))
            {
                yield return new ValidationResult(
                    "Reservation start time cannot be in the past.",
                    new[] { nameof(StartDateTime) });
            }

            var duration = EndDateTime - StartDateTime;
            if (duration.TotalMinutes < 15)
            {
                yield return new ValidationResult(
                    "Minimum reservation duration is 15 minutes.",
                    new[] { nameof(EndDateTime) });
            }

            if (duration.TotalDays > 30)
            {
                yield return new ValidationResult(
                    "Maximum reservation duration is 30 days.",
                    new[] { nameof(EndDateTime) });
            }
        }
    }

    public class BookingDetailsViewModel
    {
        public int Id { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }

        public int SlotId { get; set; }
        public string SlotNumber { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;

        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public decimal HourlyRateAtBooking { get; set; }
        public decimal EstimatedFee { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Session
        public bool HasSession => Session != null;
        public ParkingSession? Session { get; set; }

        // Action permissions
        public bool CanCancel => BookingStatus == "Confirmed" && Session == null && EndDateTime > DateTime.Now;
        public bool CanCheckIn => BookingStatus == "Confirmed" && Session == null;
        public bool CanCheckOut => Session != null && Session.Status == "CheckedIn";
    }

    public class MyBookingsViewModel
    {
        public string? FilterStatus { get; set; }
        public List<ParkingBooking> Bookings { get; set; } = new List<ParkingBooking>();
        public int TotalBookings => Bookings.Count;
        public int ActiveBookings => Bookings.Count(b => b.BookingStatus == "Confirmed");
        public int CompletedBookings => Bookings.Count(b => b.BookingStatus == "Completed");
        public int CancelledBookings => Bookings.Count(b => b.BookingStatus == "Cancelled");
    }
}
