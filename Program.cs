using AmarShowsBook.Data;
using AmarShowsBook.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<OtpDeliveryService>();
// Register RBAC permission service
builder.Services.AddScoped<RbacService>();

// Register custom activity logger service
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
// Allow ActivityLogger to access HttpContext
builder.Services.AddHttpContextAccessor();
//=== Add HttpContextAccessor for logging purposes
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
//=== End HttpContextAccessor

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
if (!connectionString.Contains("Timeout=", StringComparison.OrdinalIgnoreCase))
{
    connectionString += ";Timeout=3;Command Timeout=5";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Lifetime.ApplicationStarted.Register(() =>
{
    Task.Run(() =>
    {
        using var scope = app.Services.CreateScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            DbSeeder.Seed(db);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Database seed skipped. The app will still start and show friendly errors if the database is unavailable.");
        }
    });

    if (app.Environment.IsDevelopment())
    {
        Task.Run(() =>
        {
            try
            {
                var url = app.Urls.FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    ?? app.Urls.FirstOrDefault()
                    ?? "http://localhost:5089";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else
                {
                    Process.Start("xdg-open", url);
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Browser launch skipped.");
            }
        });
    }
});

app.Run();
