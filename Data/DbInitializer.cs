using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ParkEase.Models;

namespace ParkEase.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            // Apply migrations / ensure database is created
            await context.Database.MigrateAsync();

            // 1. Seed Roles
            string[] roles = { "Admin", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Role {Role} created.", role);
                }
            }

            // 2. Seed Admin User
            string adminEmail = "admin@parkease.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    PhoneNumber = "+91 9900011223",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger.LogInformation("Admin user {Email} seeded successfully.", adminEmail);
                }
            }

            // 3. Seed Sample Customer User
            string customerEmail = "john@example.com";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);
            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    EmailConfirmed = true,
                    FullName = "John Doe",
                    PhoneNumber = "+91 9876543210",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(customerUser, "User@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "Customer");
                    logger.LogInformation("Customer user {Email} seeded successfully.", customerEmail);
                }
            }

            // 4. Seed Parking Locations
            if (!await context.ParkingLocations.AnyAsync())
            {
                var locations = new List<ParkingLocation>
                {
                    new ParkingLocation
                    {
                        Name = "Downtown Central Plaza",
                        Address = "100 MG Road, Central Business District",
                        Description = "Multi-level covered parking with 24/7 CCTV surveillance, automated gates, and EV charging stations.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-60)
                    },
                    new ParkingLocation
                    {
                        Name = "Metro Grand Mall",
                        Address = "45 West Avenue, Near Metro Station Gate 3",
                        Description = "Convenient shopping mall parking with wide vehicle bays, digital navigation signage, and valet assistance.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-45)
                    },
                    new ParkingLocation
                    {
                        Name = "Tech Park Silicon Valley",
                        Address = "12 Cyber City Ring Road, IT Corridor",
                        Description = "Modern tech park automated parking facility tailored for corporate employees and daily visitors.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    },
                    new ParkingLocation
                    {
                        Name = "Airport Terminal Express",
                        Address = "Express Highway, Airport Terminal 2 Connector",
                        Description = "Secure long-term and short-term airport parking with complimentary luggage trolley service and shuttle.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    }
                };

                context.ParkingLocations.AddRange(locations);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded 4 Parking Locations.");
            }

            // 5. Seed Parking Slots
            if (!await context.ParkingSlots.AnyAsync())
            {
                var downtown = await context.ParkingLocations.FirstAsync(l => l.Name.Contains("Downtown"));
                var metroMall = await context.ParkingLocations.FirstAsync(l => l.Name.Contains("Metro"));
                var techPark = await context.ParkingLocations.FirstAsync(l => l.Name.Contains("Tech"));
                var airport = await context.ParkingLocations.FirstAsync(l => l.Name.Contains("Airport"));

                var slots = new List<ParkingSlot>
                {
                    // Downtown Plaza Slots
                    new ParkingSlot { SlotNumber = "A-01", ParkingLocationId = downtown.Id, VehicleType = "Car", HourlyRate = 40.00m, Description = "Level 1 - Near Elevator A", Status = "Available" },
                    new ParkingSlot { SlotNumber = "A-02", ParkingLocationId = downtown.Id, VehicleType = "Car", HourlyRate = 40.00m, Description = "Level 1 - Covered Bay", Status = "Available" },
                    new ParkingSlot { SlotNumber = "A-03", ParkingLocationId = downtown.Id, VehicleType = "SUV", HourlyRate = 60.00m, Description = "Level 1 - Wide SUV Bay", Status = "Available" },
                    new ParkingSlot { SlotNumber = "B-01", ParkingLocationId = downtown.Id, VehicleType = "Bike", HourlyRate = 15.00m, Description = "Ground Floor - Dedicated Two-Wheeler Bay", Status = "Available" },
                    new ParkingSlot { SlotNumber = "B-02", ParkingLocationId = downtown.Id, VehicleType = "Bike", HourlyRate = 15.00m, Description = "Ground Floor - Bike Bay", Status = "Available" },
                    new ParkingSlot { SlotNumber = "E-01", ParkingLocationId = downtown.Id, VehicleType = "EV", HourlyRate = 55.00m, Description = "Level 1 - Fast 50kW EV Charger", Status = "Available" },
                    new ParkingSlot { SlotNumber = "A-04", ParkingLocationId = downtown.Id, VehicleType = "Car", HourlyRate = 40.00m, Description = "Level 2 - Repaving surface", Status = "Maintenance" },

                    // Metro Grand Mall Slots
                    new ParkingSlot { SlotNumber = "M-01", ParkingLocationId = metroMall.Id, VehicleType = "Car", HourlyRate = 50.00m, Description = "Basement 1 - Cinema Hall Entry", Status = "Available" },
                    new ParkingSlot { SlotNumber = "M-02", ParkingLocationId = metroMall.Id, VehicleType = "Car", HourlyRate = 50.00m, Description = "Basement 1 - Supermarket Entrance", Status = "Available" },
                    new ParkingSlot { SlotNumber = "M-03", ParkingLocationId = metroMall.Id, VehicleType = "SUV", HourlyRate = 70.00m, Description = "Basement 1 - Extra Length Bay", Status = "Available" },
                    new ParkingSlot { SlotNumber = "MB-01", ParkingLocationId = metroMall.Id, VehicleType = "Bike", HourlyRate = 20.00m, Description = "Ground Floor East Bay", Status = "Available" },
                    new ParkingSlot { SlotNumber = "ME-01", ParkingLocationId = metroMall.Id, VehicleType = "EV", HourlyRate = 65.00m, Description = "Basement 1 - Type 2 AC EV Charger", Status = "Available" },

                    // Tech Park Slots
                    new ParkingSlot { SlotNumber = "T-01", ParkingLocationId = techPark.Id, VehicleType = "Car", HourlyRate = 35.00m, Description = "Tower A - Visitor Bay", Status = "Available" },
                    new ParkingSlot { SlotNumber = "T-02", ParkingLocationId = techPark.Id, VehicleType = "Car", HourlyRate = 35.00m, Description = "Tower B - Covered Stall", Status = "Available" },
                    new ParkingSlot { SlotNumber = "TB-01", ParkingLocationId = techPark.Id, VehicleType = "Bike", HourlyRate = 12.00m, Description = "Two-Wheeler Shed 1", Status = "Available" },
                    new ParkingSlot { SlotNumber = "TE-01", ParkingLocationId = techPark.Id, VehicleType = "EV", HourlyRate = 50.00m, Description = "Tower A - Rapid EV Charger", Status = "Available" },

                    // Airport Terminal Slots
                    new ParkingSlot { SlotNumber = "AP-01", ParkingLocationId = airport.Id, VehicleType = "Car", HourlyRate = 60.00m, Description = "Express Short Stay - 2 min walk to T2", Status = "Available" },
                    new ParkingSlot { SlotNumber = "AP-02", ParkingLocationId = airport.Id, VehicleType = "SUV", HourlyRate = 80.00m, Description = "Express Premium SUV Bay", Status = "Available" },
                    new ParkingSlot { SlotNumber = "AP-03", ParkingLocationId = airport.Id, VehicleType = "Car", HourlyRate = 60.00m, Description = "Sensor recalibration in progress", Status = "Maintenance" }
                };

                context.ParkingSlots.AddRange(slots);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded 17 Parking Slots across locations.");
            }

            // 6. Seed Sample Bookings and Sessions
            if (!await context.ParkingBookings.AnyAsync() && customerUser != null)
            {
                var slotA01 = await context.ParkingSlots.FirstAsync(s => s.SlotNumber == "A-01");
                var slotB01 = await context.ParkingSlots.FirstAsync(s => s.SlotNumber == "B-01");
                var slotM01 = await context.ParkingSlots.FirstAsync(s => s.SlotNumber == "M-01");

                // Booking 1: Upcoming Confirmed Booking
                var booking1 = new ParkingBooking
                {
                    BookingReference = "PK-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-UP1001",
                    UserId = customerUser.Id,
                    ParkingSlotId = slotA01.Id,
                    VehicleNumber = "MH12AB1234",
                    VehicleType = "Car",
                    StartDateTime = DateTime.Now.AddHours(2),
                    EndDateTime = DateTime.Now.AddHours(5),
                    HourlyRateAtBooking = 40.00m,
                    EstimatedFee = 120.00m,
                    BookingStatus = "Confirmed",
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                };

                // Booking 2: Active Check-In Session
                var booking2 = new ParkingBooking
                {
                    BookingReference = "PK-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-ACT202",
                    UserId = customerUser.Id,
                    ParkingSlotId = slotM01.Id,
                    VehicleNumber = "DL04CD5678",
                    VehicleType = "Car",
                    StartDateTime = DateTime.Now.AddHours(-1),
                    EndDateTime = DateTime.Now.AddHours(3),
                    HourlyRateAtBooking = 50.00m,
                    EstimatedFee = 200.00m,
                    BookingStatus = "Confirmed",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                };

                // Booking 3: Past Completed Booking with Session
                var booking3 = new ParkingBooking
                {
                    BookingReference = "PK-20261019-CMP303",
                    UserId = customerUser.Id,
                    ParkingSlotId = slotB01.Id,
                    VehicleNumber = "KA05XY9988",
                    VehicleType = "Bike",
                    StartDateTime = DateTime.Now.AddDays(-2).AddHours(10),
                    EndDateTime = DateTime.Now.AddDays(-2).AddHours(13),
                    HourlyRateAtBooking = 15.00m,
                    EstimatedFee = 45.00m,
                    BookingStatus = "Completed",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                };

                context.ParkingBookings.AddRange(booking1, booking2, booking3);
                await context.SaveChangesAsync();

                // Add active session for booking2
                var sessionActive = new ParkingSession
                {
                    BookingId = booking2.Id,
                    CheckInTime = DateTime.UtcNow.AddMinutes(-50),
                    Status = "CheckedIn"
                };

                // Add completed session for booking3
                var sessionCompleted = new ParkingSession
                {
                    BookingId = booking3.Id,
                    CheckInTime = DateTime.UtcNow.AddDays(-2).AddHours(10),
                    CheckOutTime = DateTime.UtcNow.AddDays(-2).AddHours(12).AddMinutes(45),
                    ActualDurationMinutes = 165,
                    FinalFee = 45.00m,
                    Status = "CheckedOut"
                };

                context.ParkingSessions.AddRange(sessionActive, sessionCompleted);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded 3 sample bookings and sessions.");
            }

            // 7. Seed Contact Messages
            if (!await context.ContactMessages.AnyAsync())
            {
                context.ContactMessages.AddRange(
                    new ContactMessage
                    {
                        Name = "Alice Smith",
                        Email = "alice@example.com",
                        Subject = "Monthly Corporate Pass Inquiry",
                        Message = "Hi ParkEase team, do you offer monthly parking passes for the Tech Park Silicon Valley location?",
                        SubmittedAt = DateTime.UtcNow.AddDays(-3),
                        Status = "New"
                    },
                    new ContactMessage
                    {
                        Name = "David Brown",
                        Email = "david@example.com",
                        Subject = "EV Charging Rate Clarification",
                        Message = "Does the hourly EV rate at Downtown Central Plaza include electricity consumption or is that charged extra?",
                        SubmittedAt = DateTime.UtcNow.AddDays(-1),
                        Status = "Read"
                    }
                );
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded sample contact messages.");
            }
        }
    }
}
