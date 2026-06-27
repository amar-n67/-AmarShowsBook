using AmarShowsBook.Data;
using AmarShowsBook.Filters;
using AmarShowsBook.Models;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

var port =
Environment.GetEnvironmentVariable("PORT")
?? "5089";

builder.WebHost.UseUrls(
$"http://0.0.0.0:{port}"
);
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
GetDatabaseConnectionString(builder.Configuration);

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

var forwardedHeadersOptions =
new ForwardedHeadersOptions
{
    ForwardedHeaders =
    ForwardedHeaders.XForwardedFor |
    ForwardedHeaders.XForwardedProto |
    ForwardedHeaders.XForwardedHost
};

forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();


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

app.UseForwardedHeaders(
forwardedHeadersOptions);

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.MapControllerRoute(
name:"default",
pattern:
"{controller=Auth}/{action=Login}/{id?}"
);

EnsureApplicationVersionTable(app);


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
$"http://localhost:{port}";


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

static string GetDatabaseConnectionString(IConfiguration configuration)
{
    var databaseUrl =
    Environment.GetEnvironmentVariable("DATABASE_URL");

    if(!string.IsNullOrWhiteSpace(databaseUrl))
    {
        return ConvertPostgresUrlToConnectionString(databaseUrl);
    }

    return configuration.GetConnectionString(
    "DefaultConnection")
    ?? "";
}

static string ConvertPostgresUrlToConnectionString(string databaseUrl)
{
    var uri =
    new Uri(databaseUrl);

    var userInfo =
    uri.UserInfo.Split(
    ':',
    2);

    var username =
    Uri.UnescapeDataString(
    userInfo.ElementAtOrDefault(0)
    ?? "");

    var password =
    Uri.UnescapeDataString(
    userInfo.ElementAtOrDefault(1)
    ?? "");

    var database =
    Uri.UnescapeDataString(
    uri.AbsolutePath.TrimStart('/'));

    var builder =
    new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = database,
        Username = username,
        Password = password,
        SslMode = Npgsql.SslMode.Require
    };

    return builder.ConnectionString;
}

static void EnsureApplicationVersionTable(WebApplication app)
{
    using var scope =
    app.Services.CreateScope();

    try
    {
        var context =
        scope.ServiceProvider
        .GetRequiredService<
        ApplicationDbContext>();

        context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS public.application_versions
(
    id bigserial PRIMARY KEY,
    version_number varchar(50) NOT NULL DEFAULT '1.0.0',
    release_title varchar(255) NOT NULL DEFAULT 'Application release',
    release_notes text,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by varchar(255),
    is_current boolean NOT NULL DEFAULT false
);

INSERT INTO public.application_versions
(
    version_number,
    release_title,
    release_notes,
    updated_at,
    created_at,
    created_by,
    is_current
)
SELECT
    '1.0.0',
    'Initial release',
    'Default application version',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    'SYSTEM',
    true
WHERE NOT EXISTS
(
    SELECT 1
    FROM public.application_versions
);
");
    }
    catch(Exception ex)
    {
        app.Logger.LogWarning(
        ex,
        "Application version schema check skipped"
        );
    }
}
