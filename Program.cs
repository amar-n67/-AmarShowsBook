using AmarShowsBook.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
if (!connectionString.Contains("Timeout=", StringComparison.OrdinalIgnoreCase))
{
    connectionString += ";Timeout=3;Command Timeout=5";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

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
});

app.Run();
