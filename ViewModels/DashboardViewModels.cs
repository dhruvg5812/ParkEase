using ParkEase.Models;
using System.ComponentModel.DataAnnotations;

namespace ParkEase.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalParkingSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int OccupiedSlots { get; set; }
        public int MaintenanceSlots { get; set; }

        public int TodayReservations { get; set; }
        public int UpcomingReservations { get; set; }
        public int ActiveCheckIns { get; set; }
        public int CompletedBookings { get; set; }

        public decimal TotalRecordedRevenue { get; set; }
        public decimal EstimatedActiveRevenue { get; set; }

        public List<ParkingBooking> RecentBookings { get; set; } = new List<ParkingBooking>();
        public List<LocationSlotStat> LocationStats { get; set; } = new List<LocationSlotStat>();
        public List<ContactMessage> RecentMessages { get; set; } = new List<ContactMessage>();
    }

    public class LocationSlotStat
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int ActiveBookings { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ContactViewModel
    {
        [Required(ErrorMessage = "Your name is required.")]
        [StringLength(100)]
        [Display(Name = "Your Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters.")]
        [Display(Name = "Your Message")]
        public string Message { get; set; } = string.Empty;
    }

    public class CustomerDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public int UpcomingBookingsCount { get; set; }
        public int ActiveCheckInsCount { get; set; }
        public decimal TotalSpent { get; set; }
        public List<ParkingBooking> UpcomingBookings { get; set; } = new List<ParkingBooking>();
        public List<ParkingBooking> RecentBookings { get; set; } = new List<ParkingBooking>();
    }
}
