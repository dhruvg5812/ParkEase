using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkEase.Models
{
    public class ParkingBooking
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Booking Reference")]
        public string BookingReference { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [Display(Name = "Parking Slot")]
        public int ParkingSlotId { get; set; }

        [ForeignKey(nameof(ParkingSlotId))]
        public virtual ParkingSlot? ParkingSlot { get; set; }

        [Required(ErrorMessage = "Vehicle number is required.")]
        [StringLength(30)]
        [Display(Name = "Vehicle Registration Number")]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; } = "Car";

        [Required(ErrorMessage = "Start date and time is required.")]
        [Display(Name = "Start Time")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "End date and time is required.")]
        [Display(Name = "End Time")]
        public DateTime EndDateTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Rate Applied (₹/hr)")]
        public decimal HourlyRateAtBooking { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Estimated Fee (₹)")]
        public decimal EstimatedFee { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Booking Status")]
        public string BookingStatus { get; set; } = "Confirmed"; // Confirmed, Pending, Cancelled, Completed

        [Display(Name = "Booked On")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Cancelled At")]
        public DateTime? CancelledAt { get; set; }

        // Navigation to session (check-in / check-out)
        public virtual ParkingSession? Session { get; set; }
    }
}
