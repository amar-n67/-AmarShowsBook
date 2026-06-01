using AmarShowsBook.Data;
using AmarShowsBook.Filters;
using AmarShowsBook.Models;
using AmarShowsBook.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseUrls(

//     "http://localhost:5089",

//     "http://0.0.0.0:5089"

// );
// var port = Environment.GetEnvironmentVariable("PORT") ?? "5089";

// builder.WebHost.UseUrls(
//     $"http://0.0.0.0:{port}"
// );

// ========================================
// Services
// ========================================

builder.Services.AddScoped<SessionAuthorizeFilter>();
builder.Services.AddScoped<ActivityLoggingFilter>();
builder.Services.AddScoped<BookingStepValidationFilter>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessionAuthorizeFilter>();
    options.Filters.Add<BookingStepValidationFilter>();
    options.Filters.Add<ActivityLoggingFilter>();
});

builder.Services.AddSingleton<OtpDeliveryService>();

builder.Services.AddScoped<RbacService>();

builder.Services.AddScoped<IActivityLogger,
ActivityLogger>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSession();


// ========================================
// Database
// ========================================

var connectionString =
builder.Configuration.GetConnectionString(
"DefaultConnection")
?? "";

if(
!connectionString.Contains(
"Timeout=",
StringComparison.OrdinalIgnoreCase))
{
    connectionString+=
    ";Timeout=3;Command Timeout=5";
}

builder.Services.AddDbContext<ApplicationDbContext>(
options=>
options.UseNpgsql(
connectionString
)
);

var app=
builder.Build();


// ========================================
// Middleware
// ========================================

if(!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
    "/Home/Error");
}
else
{
    app.UseExceptionHandler(
    "/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.MapControllerRoute(
name:"default",
pattern:
"{controller=Auth}/{action=Login}/{id?}"
);


// ========================================
// Seed Data
// ========================================

app.Lifetime.ApplicationStarted.Register(
()=>
{
Task.Run(()=>
{
using var scope=
app.Services.CreateScope();

try
{
    var context=
    scope.ServiceProvider
    .GetRequiredService<
    ApplicationDbContext>();


    // ==========================
    // Dummy card seed
    // ==========================

    if(
    !context.DummyCards.Any()
    )
    {
        context.DummyCards
        .AddRange(

        new DummyCard
        {
            CardNo=
            "4111111111111111",

            HolderName=
            "AMAR TEST",

            CVV=
            "123",

            Expiry=
            "12/27"
        },

        new DummyCard
        {
            CardNo=
            "5555555555554444",

            HolderName=
            "TEST USER",

            CVV=
            "456",

            Expiry=
            "10/28"
        },

        new DummyCard
        {
            CardNo=
            "6011111111111117",

            HolderName=
            "MOVIE USER",

            CVV=
            "789",

            Expiry=
            "09/26"
        }

        );

        context.SaveChanges();
    }


    // ==========================
    // Existing seed
    // ==========================

    DbSeeder.Seed(
    context
    );

}
catch(Exception ex)
{
    app.Logger.LogWarning(
    ex,
    "Database seed skipped"
    );
}
});


// ========================================
// Auto open browser
// ========================================

if(
app.Environment.IsDevelopment()
)
{
Task.Run(()=>
{
try
{
var url=

app.Urls
.FirstOrDefault(
u=>
u.StartsWith(
"http://",
StringComparison.OrdinalIgnoreCase
))

??

app.Urls
.FirstOrDefault()

??

"http://localhost:5089";


if(
RuntimeInformation.IsOSPlatform(
OSPlatform.OSX
))
{
Process.Start(
"open",
url
);
}
else if(
RuntimeInformation.IsOSPlatform(
OSPlatform.Windows
))
{
Process.Start(
new ProcessStartInfo(
url)
{
UseShellExecute=true
});
}
else
{
Process.Start(
"xdg-open",
url
);
}
}
catch(Exception ex)
{
app.Logger.LogWarning(
ex,
"Browser launch skipped"
);
}
});
}

});

app.Run();
