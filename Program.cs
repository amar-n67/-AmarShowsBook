using AmarShowsBook.Data;
using AmarShowsBook.Filters;
using AmarShowsBook.Helpers;
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
DatabaseConnectionStringResolver.GetDatabaseConnectionString(
builder.Configuration);

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
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //db.Database.Migrate(); //     temporarily disable automatic migrations:
}
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
//commented below to deploy onrender for temporary purpose
//EnsureApplicationVersionTable(app);
//EnsureDeveloperProfileStore(app);


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

static void EnsureDeveloperProfileStore(WebApplication app)
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
CREATE TABLE IF NOT EXISTS public.developer_profiles
(
    developer_id integer PRIMARY KEY DEFAULT 1,
    full_name text,
    email text,
    phone text,
    bio text,
    address text,
    experience_years integer NOT NULL DEFAULT 0,
    skills text,
    education text,
    projects text,
    technologies text,
    achievements text,
    resume_url text,
    profile_image text,
    github_url text,
    linked_in_url text,
    twitter_url text,
    instagram_url text,
    facebook_url text,
    youtube_url text,
    website_url text,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_developer_profiles_single_row CHECK (developer_id = 1)
);

INSERT INTO public.developer_profiles
(
    developer_id,
    full_name,
    email,
    bio,
    experience_years
)
SELECT
    1,
    'Amar',
    'example@gmail.com',
    'Developer Profile',
    0
WHERE NOT EXISTS
(
    SELECT 1
    FROM public.developer_profiles
    WHERE developer_id = 1
);

CREATE OR REPLACE VIEW public.""vwDeveloperProfile"" AS
SELECT
    developer_id AS ""DeveloperId"",
    full_name AS ""FullName"",
    email AS ""Email"",
    phone AS ""Phone"",
    bio AS ""Bio"",
    address AS ""Address"",
    experience_years AS ""ExperienceYears"",
    skills AS ""Skills"",
    education AS ""Education"",
    projects AS ""Projects"",
    technologies AS ""Technologies"",
    achievements AS ""Achievements"",
    resume_url AS ""ResumeUrl"",
    profile_image AS ""ProfileImage"",
    github_url AS ""GitHubUrl"",
    linked_in_url AS ""LinkedInUrl"",
    twitter_url AS ""TwitterUrl"",
    instagram_url AS ""InstagramUrl"",
    facebook_url AS ""FacebookUrl"",
    youtube_url AS ""YoutubeUrl"",
    website_url AS ""WebsiteUrl""
FROM public.developer_profiles;
");
    }
    catch(Exception ex)
    {
        app.Logger.LogWarning(
        ex,
        "Developer profile schema check skipped"
        );
    }
}
