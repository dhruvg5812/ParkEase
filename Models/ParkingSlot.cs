using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkEase.Models
{
    public class ParkingSlot
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Slot number is required.")]
        [StringLength(20, ErrorMessage = "Slot number cannot exceed 20 characters.")]
        [Display(Name = "Slot Number")]
        public string SlotNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parking location is required.")]
        [Display(Name = "Parking Location")]
        public int ParkingLocationId { get; set; }

        [ForeignKey(nameof(ParkingLocationId))]
        public virtual ParkingLocation? ParkingLocation { get; set; }

        [Required(ErrorMessage = "Vehicle type is required.")]
        [StringLength(20)]
        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; } = "Car"; // Car, Bike, SUV, EV

        [Required(ErrorMessage = "Hourly rate is required.")]
        [Range(0, 10000, ErrorMessage = "Hourly rate must be a non-negative amount.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Hourly Rate (₹)")]
        public decimal HourlyRate { get; set; }

        [StringLength(250)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Available"; // Available, Unavailable, Maintenance

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<ParkingBooking> Bookings { get; set; } = new List<ParkingBooking>();
    }
}
