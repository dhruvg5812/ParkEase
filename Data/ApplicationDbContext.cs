using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParkEase.Models;

namespace ParkEase.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ParkingLocation> ParkingLocations => Set<ParkingLocation>();
        public DbSet<ParkingSlot> ParkingSlots => Set<ParkingSlot>();
        public DbSet<ParkingBooking> ParkingBookings => Set<ParkingBooking>();
        public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ParkingLocation -> ParkingSlots (1 to Many)
            builder.Entity<ParkingSlot>()
                .HasOne(s => s.ParkingLocation)
                .WithMany(l => l.ParkingSlots)
                .HasForeignKey(s => s.ParkingLocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // SlotNumber must be unique within a parking location
            builder.Entity<ParkingSlot>()
                .HasIndex(s => new { s.ParkingLocationId, s.SlotNumber })
                .IsUnique();

            // ParkingBooking -> User (Many to 1)
            builder.Entity<ParkingBooking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ParkingBooking -> ParkingSlot (Many to 1)
            builder.Entity<ParkingBooking>()
                .HasOne(b => b.ParkingSlot)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ParkingSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            // BookingReference unique index
            builder.Entity<ParkingBooking>()
                .HasIndex(b => b.BookingReference)
                .IsUnique();

            // ParkingBooking -> ParkingSession (1 to 1)
            builder.Entity<ParkingSession>()
                .HasOne(ps => ps.Booking)
                .WithOne(b => b.Session)
                .HasForeignKey<ParkingSession>(ps => ps.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ParkingSession>()
                .HasIndex(ps => ps.BookingId)
                .IsUnique();
        }
    }
}
