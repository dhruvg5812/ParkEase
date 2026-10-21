using Microsoft.AspNetCore.Mvc.Rendering;
using ParkEase.Models;
using System.ComponentModel.DataAnnotations;

namespace ParkEase.ViewModels
{
    public class ParkingSearchViewModel
    {
        [Display(Name = "Location")]
        public int? LocationId { get; set; }

        [Display(Name = "Vehicle Type")]
        public string? VehicleType { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime? SearchDate { get; set; }

        [Display(Name = "Start Time")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "End Time")]
        public TimeSpan? EndTime { get; set; }

        public List<SelectListItem> Locations { get; set; } = new List<SelectListItem>();

        public List<ParkingSlotCardViewModel> Slots { get; set; } = new List<ParkingSlotCardViewModel>();

        public int TotalFound => Slots.Count;
        public int AvailableCount => Slots.Count(s => s.IsAvailableNow);
    }

    public class ParkingSlotCardViewModel
    {
        public int Id { get; set; }
        public string SlotNumber { get; set; } = string.Empty;
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Available"; // Available, Unavailable, Maintenance
        public bool IsAvailableForSearch { get; set; } = true;
        public bool IsAvailableNow { get; set; } = true;
    }
}
