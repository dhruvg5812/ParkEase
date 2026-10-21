using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkEase.Models
{
    public class ParkingSession
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public virtual ParkingBooking? Booking { get; set; }

        [Required]
        [Display(Name = "Check-In Time")]
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;

        [Display(Name = "Check-Out Time")]
        public DateTime? CheckOutTime { get; set; }

        [Display(Name = "Actual Duration (Minutes)")]
        public int? ActualDurationMinutes { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Final Fee (₹)")]
        public decimal? FinalFee { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Session Status")]
        public string Status { get; set; } = "CheckedIn"; // CheckedIn, CheckedOut
    }
}
