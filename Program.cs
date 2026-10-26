using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ParkEase.Data;
using ParkEase.Models;
using ParkEase.Services;

var builder = WebApplication.CreateBuilder(args);

// Provider and Connection String
var providerSetting = builder.Configuration["DatabaseProvider"] ?? "Auto";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=parkease_db;User=root;Password=;CharSet=utf8mb4;";
var sqliteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection")
    ?? "Data Source=parkease.db";

bool useMySql = false;
if (providerSetting.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    useMySql = true;
}
else if (providerSetting.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    useMySql = false;
}
else
{
    useMySql = CanConnectToMySql(connectionString);
}

// 1. Add DbContext with MySQL or SQLite fallback
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useMySql)
    {
        try
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });
        }
        catch
        {
            options.UseSqlite(sqliteConnectionString);
        }
    }
    else
    {
        options.UseSqlite(sqliteConnectionString);
    }
});

// 2. Add ASP.NET Core Identity with Roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Simplified college project password settings for easy testing
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Configure Cookie Settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "ParkEase.AuthCookie";
});

// 4. Session & State Management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "ParkEase.Session";
});

// 5. Register Application Services
builder.Services.AddScoped<IFeeCalculationService, FeeCalculationService>();
builder.Services.AddScoped<IParkingAvailabilityService, ParkingAvailabilityService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// 6. Add MVC Controllers & Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Database automatically on startup
try
{
    await DbInitializer.SeedAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during database migration or seeding.");
}

// Configure HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static bool CanConnectToMySql(string connectionString)
{
    try
    {
        var csb = new MySqlConnector.MySqlConnectionStringBuilder(connectionString);
        using var client = new System.Net.Sockets.TcpClient();
        var result = client.BeginConnect(csb.Server, (int)csb.Port, null, null);
        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(1000));
        if (!success)
        {
            return false;
        }
        client.EndConnect(result);
        return true;
    }
    catch
    {
        return false;
    }
}

