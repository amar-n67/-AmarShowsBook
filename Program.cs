using AmarShowsBook.Data;
using AmarShowsBook.Filters;
using AmarShowsBook.Helpers;
using AmarShowsBook.Models;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Diagnostics;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

const string ApplicationPort =
"9090";

builder.WebHost.UseUrls(
$"http://0.0.0.0:{ApplicationPort}"
);

// Request flow starts here: filters protect sessions, booking steps, and audit logging before MVC actions run.
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

// One database connection feeds MVC pages, RBAC views, booking tables, and admin reports.
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
.ConfigureWarnings(warnings =>
    warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
);

var app=
builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    BaselineExistingDatabase(app, db);
    db.Database.Migrate();
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

app.Use(
async (context, next) =>
{
    if (ShouldRedirectDashboardOnlyAdmin(context))
    {
        context.Response.Redirect("/Admin/Dashboard");
        return;
    }

    await next();
});

app.MapControllerRoute(
name:"default",
pattern:
"{controller=Auth}/{action=Login}/{id?}"
);

// Startup keeps old databases usable by creating the small tables and views that newer pages depend on.
EnsureApplicationVersionTable(app);
EnsureDeveloperProfileStore(app);
EnsureAmaroChatStore(app);
EnsureRbacStore(app);
EnsureNewsStore(app);
EnsureAccountDeletionArchiveStore(app);

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


    // Local payment testing expects these cards to exist.
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


    // Main seed adds demo shows, locations, coupons, and wallet starter data.
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


// Development convenience only: open the local site after Kestrel starts.
if(
app.Environment.IsDevelopment()
)
{
Task.Run(()=>
{
try
{
var url=
$"http://localhost:{ApplicationPort}";


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

static bool ShouldRedirectDashboardOnlyAdmin(HttpContext context)
{
    if (IsDashboardOnlyAdminAllowedPath(context.Request.Path))
    {
        return false;
    }

    if (!int.TryParse(context.Session.GetString("UserId"), out var userId))
    {
        return false;
    }

    var rbacService = context.RequestServices.GetRequiredService<RbacService>();

    return rbacService.HasAnyActiveRole(userId, "DUM_ADMIN") &&
        !rbacService.HasAnyActiveRole(
            userId,
            "AMAR_SUPER_ADMIN",
            "AMAR_ADMIN",
            "AMAR_DEVELOPER");
}

static bool IsDashboardOnlyAdminAllowedPath(PathString path)
{
    var value = path.Value ?? "/";

    return value.StartsWith("/Admin/Dashboard", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("/Admin", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/Users", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/Bookings", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/Security", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/Transactions", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/TransactionDetails", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/Refunds", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/RefundDetails", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/CouponUsage", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/Wallets", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/Notifications", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/ActivityLogs", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Admin/Versions", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Developer", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Profile/MyProfile", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Profile/ChangePassword", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Auth/Login", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Auth/Logout", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Auth/RecoverDeletedAccount", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Auth/ForgotPassword", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("/Auth/ResetPassword", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
}

static void BaselineExistingDatabase(WebApplication app, ApplicationDbContext context)
{
    try
    {
        context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS public.""__EFMigrationsHistory""
(
    ""MigrationId"" character varying(150) NOT NULL,
    ""ProductVersion"" character varying(32) NOT NULL,
    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
);

INSERT INTO public.""__EFMigrationsHistory""
(
    ""MigrationId"",
    ""ProductVersion""
)
SELECT
    '20260602073325_InitialCreate',
    '10.0.0'
WHERE EXISTS
(
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = 'public'
    AND table_name = 'activity_logs'
)
AND NOT EXISTS
(
    SELECT 1
    FROM public.""__EFMigrationsHistory""
    WHERE ""MigrationId"" = '20260602073325_InitialCreate'
);
");
    }
    catch(Exception ex)
    {
        app.Logger.LogWarning(
        ex,
        "Existing database baseline check skipped"
        );
    }
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
    support_phone text,
    support_email text,
    support_whatsapp_text text,
    support_whatsapp_phone text,
    is_support_whatsapp_same_as_phone boolean NOT NULL DEFAULT true,
    top_whatsapp_text text,
    support_email_subject text,
    support_email_text text,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_developer_profiles_single_row CHECK (developer_id = 1)
);

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_phone text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_email text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_whatsapp_text text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_whatsapp_phone text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS is_support_whatsapp_same_as_phone boolean NOT NULL DEFAULT true;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS top_whatsapp_text text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_email_subject text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_email_text text;

INSERT INTO public.developer_profiles
(
    developer_id,
    full_name,
    email,
    bio,
    experience_years,
    twitter_url,
    support_phone,
    support_email,
    support_whatsapp_text,
    support_whatsapp_phone,
    is_support_whatsapp_same_as_phone,
    top_whatsapp_text,
    support_email_subject,
    support_email_text
)
SELECT
    1,
    'showTime Team',
    'example@gmail.com',
    'Developer Profile',
    0,
    '',
    '+91 9651698863',
    'support@showtime.com',
    'Hi showTime Team, I''m {{user}}. I need support. Please help me with my request.',
    '+91 9651698863',
    true,
    'Hi showTime Team, I''m {{user}}. I visited showTime and would like to connect with you.',
    'showTime Support Request',
    'Hi showTime Team, I''m {{user}}. I need support. Please help me with my request.'
WHERE NOT EXISTS
(
    SELECT 1
    FROM public.developer_profiles
    WHERE developer_id = 1
);

UPDATE public.developer_profiles
SET
    twitter_url = COALESCE(NULLIF(twitter_url, ''), ''),
    support_phone = COALESCE(NULLIF(support_phone, ''), '+91 9651698863'),
    support_email = COALESCE(NULLIF(support_email, ''), 'support@showtime.com'),
    support_whatsapp_text = COALESCE(NULLIF(support_whatsapp_text, ''), 'Hi showTime Team, I''m {{user}}. I need support. Please help me with my request.'),
    support_whatsapp_phone = COALESCE(NULLIF(support_whatsapp_phone, ''), NULLIF(support_phone, ''), '+91 9651698863'),
    top_whatsapp_text = COALESCE(NULLIF(top_whatsapp_text, ''), 'Hi showTime Team, I''m {{user}}. I visited showTime and would like to connect with you.'),
    support_email_subject = COALESCE(NULLIF(support_email_subject, ''), 'showTime Support Request'),
    support_email_text = COALESCE(NULLIF(support_email_text, ''), 'Hi showTime Team, I''m {{user}}. I need support. Please help me with my request.')
WHERE developer_id = 1;

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
    website_url AS ""WebsiteUrl"",
    support_phone AS ""SupportPhone"",
    support_email AS ""SupportEmail"",
    support_whatsapp_text AS ""SupportWhatsAppText"",
    support_whatsapp_phone AS ""SupportWhatsAppPhone"",
    is_support_whatsapp_same_as_phone AS ""IsSupportWhatsAppSameAsPhone"",
    top_whatsapp_text AS ""TopWhatsAppText"",
    support_email_subject AS ""SupportEmailSubject"",
    support_email_text AS ""SupportEmailText""
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

static void EnsureNewsStore(WebApplication app)
{
    using var scope =
    app.Services.CreateScope();

    try
    {
        var context =
        scope.ServiceProvider
        .GetRequiredService<
        ApplicationDbContext>();

        var sqlPath = Path.Combine(
            app.Environment.ContentRootPath,
            "Database",
            "ensure_news_channels.sql");

        if (File.Exists(sqlPath))
        {
            context.Database.ExecuteSqlRaw(File.ReadAllText(sqlPath));
            return;
        }

        context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS public.news_channels
(
    id bigserial PRIMARY KEY,
    channel_code varchar(80) NOT NULL UNIQUE,
    channel_name varchar(180) NOT NULL,
    language varchar(80) NOT NULL,
    category varchar(80) NOT NULL,
    region varchar(120) NOT NULL,
    country varchar(120) NOT NULL DEFAULT 'India',
    state varchar(120) NOT NULL DEFAULT 'All',
    city varchar(120) NOT NULL DEFAULT 'All',
    description text,
    logo_url text,
    website_url text,
    live_url text,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS public.news_broadcast_slots
(
    id bigserial PRIMARY KEY,
    channel_id bigint NOT NULL REFERENCES public.news_channels(id) ON DELETE CASCADE,
    program_title varchar(180) NOT NULL,
    program_type varchar(80) NOT NULL,
    starts_at timestamp with time zone NOT NULL,
    ends_at timestamp with time zone NOT NULL,
    is_live boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE public.news_channels ADD COLUMN IF NOT EXISTS country varchar(120) NOT NULL DEFAULT 'India';
ALTER TABLE public.news_channels ADD COLUMN IF NOT EXISTS state varchar(120) NOT NULL DEFAULT 'All';
ALTER TABLE public.news_channels ADD COLUMN IF NOT EXISTS city varchar(120) NOT NULL DEFAULT 'All';
");
    }
    catch(Exception ex)
    {
        app.Logger.LogWarning(
        ex,
        "News schema check skipped"
        );
    }
}

static void EnsureAmaroChatStore(WebApplication app)
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
CREATE TABLE IF NOT EXISTS public.amaro_chat_sessions
(
    id bigserial PRIMARY KEY,
    user_id integer,
    session_key varchar(120) NOT NULL,
    started_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_seen_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS public.amaro_chat_messages
(
    id bigserial PRIMARY KEY,
    user_id integer,
    user_message text NOT NULL,
    amaro_reply text NOT NULL,
    request_path text,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_amaro_chat_messages_user_created
ON public.amaro_chat_messages(user_id, created_at DESC);
");
    }
    catch(Exception ex)
    {
        app.Logger.LogWarning(
        ex,
        "Assistant chat schema check skipped"
        );
    }
}

static void EnsureAccountDeletionArchiveStore(WebApplication app)
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
CREATE TABLE IF NOT EXISTS public.user_account_archives
(
    id bigserial PRIMARY KEY,
    original_user_id bigint NOT NULL,
    email text NOT NULL,
    mobile text,
    password_hash text,
    deleted_by text,
    deletion_reason text,
    deleted_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    recover_until timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP + INTERVAL '30 days'),
    purge_after timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP + INTERVAL '3 months'),
    status varchar(30) NOT NULL DEFAULT 'DELETED',
    recovered_at timestamp with time zone,
    recovered_by text,
    purged_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_user_account_archives_original_user_id
ON public.user_account_archives(original_user_id);

CREATE INDEX IF NOT EXISTS ix_user_account_archives_email_status
ON public.user_account_archives(lower(email), status);

CREATE TABLE IF NOT EXISTS public.user_account_archive_records
(
    id bigserial PRIMARY KEY,
    archive_id bigint NOT NULL REFERENCES public.user_account_archives(id) ON DELETE CASCADE,
    original_user_id bigint NOT NULL,
    table_schema text NOT NULL DEFAULT 'public',
    table_name text NOT NULL,
    pk_column text NOT NULL,
    pk_value text NOT NULL,
    record_data jsonb NOT NULL,
    archived_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(archive_id, table_schema, table_name, pk_value)
);

CREATE INDEX IF NOT EXISTS ix_user_account_archive_records_archive_id
ON public.user_account_archive_records(archive_id);

CREATE OR REPLACE FUNCTION public.fn_archive_user_account(
    p_user_id bigint,
    p_deleted_by text,
    p_reason text DEFAULT NULL
)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_archive_id bigint;
    v_user public.""Users""%ROWTYPE;
    v_row record;
    v_sql text;
BEGIN
    SELECT *
    INTO v_user
    FROM public.""Users""
    WHERE ""Id"" = p_user_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'User % was not found.', p_user_id;
    END IF;

    SELECT id
    INTO v_archive_id
    FROM public.user_account_archives
    WHERE original_user_id = p_user_id
      AND status = 'DELETED'
    ORDER BY deleted_at DESC
    LIMIT 1;

    IF v_archive_id IS NULL THEN
        INSERT INTO public.user_account_archives
        (
            original_user_id,
            email,
            mobile,
            password_hash,
            deleted_by,
            deletion_reason,
            status
        )
        VALUES
        (
            p_user_id,
            v_user.""Email"",
            v_user.""Mobile"",
            v_user.""Password"",
            p_deleted_by,
            p_reason,
            'DELETED'
        )
        RETURNING id INTO v_archive_id;
    END IF;

    INSERT INTO public.""DeletedUsers""
    (
        original_user_id,
        name,
        email,
        mobile,
        address,
        country,
        state,
        district,
        pincode,
        language,
        genre,
        profile_image_path,
        created_at,
        updated_at,
        deleted_at,
        deleted_by,
        is_revoked
    )
    SELECT
        v_user.""Id"",
        v_user.""Name"",
        v_user.""Email"",
        v_user.""Mobile"",
        v_user.""Address"",
        v_user.""Country"",
        v_user.""State"",
        v_user.""District"",
        v_user.""Pincode"",
        v_user.""Language"",
        v_user.""Genre"",
        v_user.""ProfileImagePath"",
        v_user.""CreatedAt"",
        v_user.""UpdatedAt"",
        CURRENT_TIMESTAMP,
        p_deleted_by,
        false
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM public.""DeletedUsers""
        WHERE original_user_id = p_user_id
          AND is_revoked = false
    );

    FOR v_row IN
        SELECT *
        FROM (VALUES
            ('Users', 'Id', 'Id'),
            ('booking_drafts', 'UserId', 'Id'),
            ('bookings', 'user_id', 'id'),
            ('transactions', 'user_id', 'id'),
            ('refunds', 'user_id', 'id'),
            ('coupon_usage', 'user_id', 'id'),
            ('invoices', 'user_id', 'id'),
            ('loyalty_history', 'user_id', 'id'),
            ('seat_locks', 'user_id', 'id'),
            ('user_notifications', 'user_id', 'id'),
            ('user_role_mappings', 'user_id', 'id'),
            ('user_roles', 'user_id', 'id'),
            ('user_wallets', 'user_id', 'id'),
            ('wallet_transactions', 'user_id', 'id'),
            ('wallet_status_history', 'user_id', 'id'),
            ('ticket_validation_logs', 'user_id', 'id'),
            ('activity_logs', 'user_id', 'id'),
            ('amaro_chat_sessions', 'user_id', 'id'),
            ('amaro_chat_messages', 'user_id', 'id')
        ) AS archive_table(table_name, user_column, pk_column)
    LOOP
        IF EXISTS
        (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = v_row.table_name
              AND column_name = v_row.user_column
        )
        AND EXISTS
        (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = v_row.table_name
              AND column_name = v_row.pk_column
        )
        THEN
            v_sql := format(
                'INSERT INTO public.user_account_archive_records
                 (archive_id, original_user_id, table_schema, table_name, pk_column, pk_value, record_data)
                 SELECT $1, $2, %L, %L, %L, t.%I::text, to_jsonb(t)
                 FROM public.%I t
                 WHERE t.%I = $2
                 ON CONFLICT DO NOTHING',
                'public',
                v_row.table_name,
                v_row.pk_column,
                v_row.pk_column,
                v_row.table_name,
                v_row.user_column);

            EXECUTE v_sql USING v_archive_id, p_user_id;
        END IF;
    END LOOP;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'booking_items',
        'id',
        bi.id::text,
        to_jsonb(bi)
    FROM public.booking_items bi
    JOIN public.bookings b ON b.id = bi.booking_id
    WHERE b.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'booked_seats',
        'id',
        bs.id::text,
        to_jsonb(bs)
    FROM public.booked_seats bs
    JOIN public.bookings b ON b.id = bs.booking_id
    WHERE b.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'booking_status_history',
        'id',
        bsh.id::text,
        to_jsonb(bsh)
    FROM public.booking_status_history bsh
    JOIN public.bookings b ON b.id = bsh.booking_id
    WHERE b.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'booking_seats',
        'id',
        bs.id::text,
        to_jsonb(bs)
    FROM public.booking_seats bs
    JOIN public.bookings b ON b.id = bs.booking_id
    WHERE b.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'tickets',
        'id',
        t.id::text,
        to_jsonb(t)
    FROM public.tickets t
    JOIN public.bookings b ON b.id = t.booking_id
    WHERE b.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'PaymentSessions',
        'Id',
        ps.""Id""::text,
        to_jsonb(ps)
    FROM public.""PaymentSessions"" ps
    JOIN public.booking_drafts bd ON bd.""Id"" = ps.""BookingId""
    WHERE bd.""UserId"" = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'invoice_items',
        'id',
        ii.id::text,
        to_jsonb(ii)
    FROM public.invoice_items ii
    JOIN public.invoices i ON i.id = ii.invoice_id
    WHERE i.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'invoice_audit_logs',
        'id',
        ial.id::text,
        to_jsonb(ial)
    FROM public.invoice_audit_logs ial
    JOIN public.invoices i ON i.id = ial.invoice_id
    WHERE i.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'refund_history',
        'id',
        rh.id::text,
        to_jsonb(rh)
    FROM public.refund_history rh
    JOIN public.refunds r ON r.id = rh.refund_id
    WHERE r.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'refund_invoices',
        'id',
        ri.id::text,
        to_jsonb(ri)
    FROM public.refund_invoices ri
    JOIN public.refunds r ON r.id = ri.refund_id
    WHERE r.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    INSERT INTO public.user_account_archive_records
    (
        archive_id,
        original_user_id,
        table_schema,
        table_name,
        pk_column,
        pk_value,
        record_data
    )
    SELECT
        v_archive_id,
        p_user_id,
        'public',
        'notification_audit_logs',
        'id',
        nal.id::text,
        to_jsonb(nal)
    FROM public.notification_audit_logs nal
    JOIN public.user_notifications un ON un.id = nal.notification_id
    WHERE un.user_id = p_user_id
    ON CONFLICT DO NOTHING;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'notification_audit_logs') THEN
        DELETE FROM public.notification_audit_logs nal
        USING public.user_notifications un
        WHERE un.id = nal.notification_id
          AND un.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'invoice_audit_logs') THEN
        DELETE FROM public.invoice_audit_logs ial
        USING public.invoices i
        WHERE i.id = ial.invoice_id
          AND i.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'invoice_items') THEN
        DELETE FROM public.invoice_items ii
        USING public.invoices i
        WHERE i.id = ii.invoice_id
          AND i.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'refund_invoices') THEN
        DELETE FROM public.refund_invoices ri
        USING public.refunds r
        WHERE r.id = ri.refund_id
          AND r.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'refund_history') THEN
        DELETE FROM public.refund_history rh
        USING public.refunds r
        WHERE r.id = rh.refund_id
          AND r.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'booked_seats') THEN
        DELETE FROM public.booked_seats bs
        USING public.bookings b
        WHERE b.id = bs.booking_id
          AND b.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'booking_seats') THEN
        DELETE FROM public.booking_seats bs
        USING public.bookings b
        WHERE b.id = bs.booking_id
          AND b.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'booking_items') THEN
        DELETE FROM public.booking_items bi
        USING public.bookings b
        WHERE b.id = bi.booking_id
          AND b.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'tickets') THEN
        DELETE FROM public.tickets t
        USING public.bookings b
        WHERE b.id = t.booking_id
          AND b.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'booking_status_history') THEN
        DELETE FROM public.booking_status_history bsh
        USING public.bookings b
        WHERE b.id = bsh.booking_id
          AND b.user_id = p_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'PaymentSessions') THEN
        DELETE FROM public.""PaymentSessions"" ps
        USING public.booking_drafts bd
        WHERE bd.""Id"" = ps.""BookingId""
          AND bd.""UserId"" = p_user_id;
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'bookings'
          AND column_name = 'transaction_id'
    )
    THEN
        UPDATE public.bookings
        SET transaction_id = NULL
        WHERE user_id = p_user_id
           OR transaction_id IN
              (
                  SELECT id
                  FROM public.transactions
                  WHERE user_id = p_user_id
              );
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'transactions'
          AND column_name = 'booking_id'
    )
    THEN
        UPDATE public.transactions
        SET booking_id = NULL
        WHERE user_id = p_user_id
           OR booking_id IN
              (
                  SELECT id
                  FROM public.bookings
                  WHERE user_id = p_user_id
              );
    END IF;

    FOR v_row IN
        SELECT *
        FROM (VALUES
            ('amaro_chat_messages', 'user_id'),
            ('loyalty_history', 'user_id'),
            ('coupon_usage', 'user_id'),
            ('invoices', 'user_id'),
            ('ticket_validation_logs', 'user_id'),
            ('wallet_status_history', 'user_id'),
            ('wallet_transactions', 'user_id'),
            ('user_notifications', 'user_id'),
            ('user_role_mappings', 'user_id'),
            ('user_roles', 'user_id'),
            ('user_wallets', 'user_id'),
            ('seat_locks', 'user_id'),
            ('refunds', 'user_id'),
            ('bookings', 'user_id'),
            ('transactions', 'user_id'),
            ('activity_logs', 'user_id'),
            ('amaro_chat_sessions', 'user_id'),
            ('booking_drafts', 'UserId')
        ) AS delete_table(table_name, user_column)
    LOOP
        IF EXISTS
        (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = v_row.table_name
              AND column_name = v_row.user_column
        )
        THEN
            v_sql := format(
                'DELETE FROM public.%I WHERE %I = $1',
                v_row.table_name,
                v_row.user_column);

            EXECUTE v_sql USING p_user_id;
        END IF;
    END LOOP;

    UPDATE public.""Users""
    SET
        ""Name"" = 'Deleted User',
        ""Email"" = 'deleted+' || p_user_id || '@deleted.local',
        ""Mobile"" = '0000000000',
        ""Address"" = NULL,
        ""Country"" = NULL,
        ""State"" = NULL,
        ""District"" = NULL,
        ""Pincode"" = NULL,
        ""Genre"" = 'Deleted',
        ""Language"" = NULL,
        ""ProfileImagePath"" = NULL,
        is_active = false,
        is_deleted = true,
        ""UpdatedAt"" = CURRENT_TIMESTAMP,
        ""UpdatedBy"" = COALESCE(p_deleted_by, 'ACCOUNT_DELETE')
    WHERE ""Id"" = p_user_id;

    RETURN v_archive_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_recover_user_account(
    p_archive_id bigint,
    p_recovered_by text
)
RETURNS boolean
LANGUAGE plpgsql
AS $$
DECLARE
    v_archive public.user_account_archives%ROWTYPE;
    v_deleted public.""DeletedUsers""%ROWTYPE;
    v_user_record jsonb;
    v_record record;
    v_exists boolean;
    v_sql text;
BEGIN
    SELECT *
    INTO v_archive
    FROM public.user_account_archives
    WHERE id = p_archive_id
      AND status = 'DELETED'
    ORDER BY deleted_at DESC
    LIMIT 1;

    IF NOT FOUND THEN
        RETURN false;
    END IF;

    IF v_archive.recover_until < CURRENT_TIMESTAMP THEN
        RETURN false;
    END IF;

    SELECT *
    INTO v_deleted
    FROM public.""DeletedUsers""
    WHERE original_user_id = v_archive.original_user_id
      AND is_revoked = false
    ORDER BY deleted_at DESC
    LIMIT 1;

    SELECT record_data
    INTO v_user_record
    FROM public.user_account_archive_records
    WHERE archive_id = p_archive_id
      AND table_name = 'Users'
    ORDER BY archived_at DESC
    LIMIT 1;

    UPDATE public.""Users""
    SET
        ""Name"" = COALESCE(v_user_record->>'Name', v_deleted.name, ""Name""),
        ""Email"" = COALESCE(v_archive.email, v_deleted.email, ""Email""),
        ""Mobile"" = COALESCE(v_archive.mobile, v_deleted.mobile, ""Mobile""),
        ""Password"" = COALESCE(v_archive.password_hash, v_user_record->>'Password', ""Password""),
        ""Address"" = COALESCE(v_user_record->>'Address', v_deleted.address),
        ""Country"" = COALESCE(v_user_record->>'Country', v_deleted.country),
        ""State"" = COALESCE(v_user_record->>'State', v_deleted.state),
        ""District"" = COALESCE(v_user_record->>'District', v_deleted.district),
        ""Pincode"" = COALESCE(v_user_record->>'Pincode', v_deleted.pincode),
        ""Genre"" = COALESCE(v_user_record->>'Genre', v_deleted.genre, 'Dramatic'),
        ""Language"" = COALESCE(v_user_record->>'Language', v_deleted.language, 'English'),
        ""ProfileImagePath"" = COALESCE(v_user_record->>'ProfileImagePath', v_deleted.profile_image_path),
        is_active = true,
        is_deleted = false,
        ""UpdatedAt"" = CURRENT_TIMESTAMP,
        ""UpdatedBy"" = COALESCE(p_recovered_by, 'ACCOUNT_RECOVERY')
    WHERE ""Id"" = v_archive.original_user_id;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'wallet_transactions') THEN
        DELETE FROM public.wallet_transactions
        WHERE user_id = v_archive.original_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'wallet_status_history') THEN
        DELETE FROM public.wallet_status_history
        WHERE user_id = v_archive.original_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'loyalty_history') THEN
        DELETE FROM public.loyalty_history
        WHERE user_id = v_archive.original_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'user_role_mappings') THEN
        DELETE FROM public.user_role_mappings
        WHERE user_id = v_archive.original_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'user_roles') THEN
        DELETE FROM public.user_roles
        WHERE user_id = v_archive.original_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'user_wallets') THEN
        DELETE FROM public.user_wallets
        WHERE user_id = v_archive.original_user_id;
    END IF;

    FOR v_record IN
        SELECT *
        FROM public.user_account_archive_records
        WHERE archive_id = p_archive_id
          AND table_name = 'transactions'
        ORDER BY id
    LOOP
        SELECT EXISTS (SELECT 1 FROM public.transactions WHERE id::text = v_record.pk_value)
        INTO v_exists;

        IF NOT v_exists THEN
            INSERT INTO public.transactions
            SELECT *
            FROM jsonb_populate_record(
                NULL::public.transactions,
                jsonb_set(v_record.record_data, '{{booking_id}}', 'null'::jsonb, true));
        END IF;
    END LOOP;

    FOR v_record IN
        SELECT *
        FROM public.user_account_archive_records
        WHERE archive_id = p_archive_id
          AND table_name = 'bookings'
        ORDER BY id
    LOOP
        SELECT EXISTS (SELECT 1 FROM public.bookings WHERE id::text = v_record.pk_value)
        INTO v_exists;

        IF NOT v_exists THEN
            INSERT INTO public.bookings
            SELECT *
            FROM jsonb_populate_record(NULL::public.bookings, v_record.record_data);
        END IF;
    END LOOP;

    UPDATE public.transactions t
    SET booking_id = (records.record_data->>'booking_id')::bigint
    FROM public.user_account_archive_records records
    WHERE records.archive_id = p_archive_id
      AND records.table_name = 'transactions'
      AND records.record_data->>'booking_id' IS NOT NULL
      AND t.id::text = records.pk_value
      AND EXISTS
      (
          SELECT 1
          FROM public.bookings b
          WHERE b.id = (records.record_data->>'booking_id')::bigint
      );

    FOR v_record IN
        SELECT *
        FROM public.user_account_archive_records
        WHERE archive_id = p_archive_id
          AND table_name NOT IN ('Users', 'transactions', 'bookings')
        ORDER BY
            CASE table_name
                WHEN 'booking_drafts' THEN 10
                WHEN 'transactions' THEN 20
                WHEN 'user_wallets' THEN 30
                WHEN 'bookings' THEN 40
                WHEN 'user_roles' THEN 50
                WHEN 'user_role_mappings' THEN 60
                WHEN 'refunds' THEN 65
                WHEN 'wallet_transactions' THEN 70
                WHEN 'coupon_usage' THEN 90
                WHEN 'invoices' THEN 100
                WHEN 'invoice_items' THEN 110
                WHEN 'invoice_audit_logs' THEN 120
                WHEN 'refund_history' THEN 130
                WHEN 'refund_invoices' THEN 140
                WHEN 'seat_locks' THEN 150
                WHEN 'wallet_status_history' THEN 160
                WHEN 'loyalty_history' THEN 170
                WHEN 'user_notifications' THEN 180
                WHEN 'notification_audit_logs' THEN 190
                WHEN 'amaro_chat_sessions' THEN 200
                WHEN 'amaro_chat_messages' THEN 210
                WHEN 'booking_items' THEN 220
                WHEN 'booking_seats' THEN 230
                WHEN 'booking_status_history' THEN 240
                WHEN 'tickets' THEN 250
                WHEN 'booked_seats' THEN 260
                WHEN 'PaymentSessions' THEN 270
                ELSE 500
            END,
            id
    LOOP
        IF EXISTS
        (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = v_record.table_schema
              AND table_name = v_record.table_name
              AND column_name = v_record.pk_column
        )
        THEN
            v_sql := format(
                'SELECT EXISTS (SELECT 1 FROM %I.%I WHERE %I::text = $1)',
                v_record.table_schema,
                v_record.table_name,
                v_record.pk_column);
            EXECUTE v_sql INTO v_exists USING v_record.pk_value;

            IF NOT v_exists THEN
                v_sql := format(
                    'INSERT INTO %I.%I SELECT * FROM jsonb_populate_record(NULL::%I.%I, $1)',
                    v_record.table_schema,
                    v_record.table_name,
                    v_record.table_schema,
                    v_record.table_name);
                EXECUTE v_sql USING v_record.record_data;
            END IF;
        END IF;
    END LOOP;

    UPDATE public.""DeletedUsers""
    SET
        is_revoked = true,
        revoke_at = CURRENT_TIMESTAMP,
        revoked_by = COALESCE(p_recovered_by, 'ACCOUNT_RECOVERY')
    WHERE original_user_id = v_archive.original_user_id
      AND is_revoked = false;

    UPDATE public.user_account_archives
    SET
        status = 'RECOVERED',
        recovered_at = CURRENT_TIMESTAMP,
        recovered_by = COALESCE(p_recovered_by, 'ACCOUNT_RECOVERY')
    WHERE id = p_archive_id;

    RETURN true;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_purge_expired_user_account_archives()
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_count integer;
BEGIN
    UPDATE public.user_account_archives
    SET
        status = 'PURGED',
        purged_at = CURRENT_TIMESTAMP
    WHERE status = 'DELETED'
      AND purge_after < CURRENT_TIMESTAMP;

    GET DIAGNOSTICS v_count = ROW_COUNT;

    DELETE FROM public.user_account_archive_records records
    USING public.user_account_archives archives
    WHERE records.archive_id = archives.id
      AND archives.status = 'PURGED';

    DELETE FROM public.""DeletedUsers""
    WHERE is_revoked = false
      AND deleted_at < CURRENT_TIMESTAMP - INTERVAL '3 months';

    RETURN v_count;
END;
$$;

SELECT public.fn_purge_expired_user_account_archives();
");
    }
    catch(Exception ex)
    {
        app.Logger.LogWarning(
        ex,
        "Account deletion archive schema check skipped"
        );
    }
}

static void EnsureRbacStore(WebApplication app)
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
CREATE OR REPLACE FUNCTION public.fn_assign_default_role()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_role_id bigint;
BEGIN
    SELECT id INTO v_role_id
    FROM public.roles
    WHERE role_code = 'AMAR_USER'
    LIMIT 1;

    IF v_role_id IS NOT NULL THEN
        INSERT INTO public.user_roles (user_id, role_id, assigned_by, is_active)
        VALUES (NEW.""Id"", v_role_id, NULL, true)
        ON CONFLICT DO NOTHING;

        INSERT INTO public.user_role_mappings (user_id, role_id, assigned_by, is_active)
        VALUES (NEW.""Id"", v_role_id, NULL, true)
        ON CONFLICT DO NOTHING;
    END IF;

    INSERT INTO public.user_wallets (user_id, wallet_balance, blocked_balance, loyalty_points, wallet_status)
    VALUES (NEW.""Id"", 0, 0, 0, 'ACTIVE')
    ON CONFLICT (user_id) DO NOTHING;

    RETURN NEW;
END;
$$;

INSERT INTO public.roles
(
    role_code,
    role_name,
    role_description,
    is_system_role,
    is_active,
    created_at,
    updated_at
)
VALUES
    ('AMAR_SUPER_ADMIN', 'Super Admin', 'Full access to every application module, including developer tools.', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('AMAR_ADMIN', 'Administrator', 'Administrative access to operations, users, shows, bookings, payments, refunds, wallet, coupons, notifications, scanner, and analytics.', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('AMAR_DEVELOPER', 'Developer', 'Developer profile and developer-only editor access.', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('AMAR_USER', 'User', 'Default customer role for booking and profile workflows.', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('DUM_ADMIN', 'dum_Admin', 'Dashboard access with Developer Profile and My Profile.', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (role_code) DO UPDATE
SET role_name = EXCLUDED.role_name,
    role_description = EXCLUDED.role_description,
    is_system_role = true,
    is_active = true,
    updated_at = CURRENT_TIMESTAMP;

INSERT INTO public.application_modules
(
    module_code,
    module_name,
    route_path,
    icon_name,
    display_order,
    is_active,
    created_at
)
VALUES
    ('ADMIN', 'Admin Dashboard', '/Admin/Dashboard', 'admin', 10, true, CURRENT_TIMESTAMP),
    ('USER', 'Users and Profiles', '/Admin/Users', 'user', 20, true, CURRENT_TIMESTAMP),
    ('ROLE', 'Roles', '/Admin/Roles', 'role', 30, true, CURRENT_TIMESTAMP),
    ('PERMISSION', 'Permissions', '/Admin/Roles', 'permission', 40, true, CURRENT_TIMESTAMP),
    ('SHOW', 'Manage Shows', '/Admin/ManageShows', 'show', 50, true, CURRENT_TIMESTAMP),
    ('BOOKING', 'Bookings and Tickets', '/Admin/Bookings', 'booking', 60, true, CURRENT_TIMESTAMP),
    ('PAYMENT', 'Payments', '/Admin/Transactions', 'payment', 70, true, CURRENT_TIMESTAMP),
    ('REFUND', 'Refunds', '/Admin/Refunds', 'refund', 80, true, CURRENT_TIMESTAMP),
    ('WALLET', 'Wallets', '/Admin/Wallets', 'wallet', 90, true, CURRENT_TIMESTAMP),
    ('COUPON', 'Coupons', '/Admin/CouponUsage', 'coupon', 100, true, CURRENT_TIMESTAMP),
    ('NOTIFICATION', 'Notifications', '/Admin/Notifications', 'notification', 110, true, CURRENT_TIMESTAMP),
    ('ANALYTICS', 'Analytics', '/Admin/Dashboard', 'analytics', 120, true, CURRENT_TIMESTAMP),
    ('SUPPORT', 'Support', '/Admin/UserAccess', 'support', 130, true, CURRENT_TIMESTAMP),
    ('SCANNER', 'Ticket Scanner', '/Admin/Security', 'scanner', 140, true, CURRENT_TIMESTAMP),
    ('DEVELOPER', 'Developer Editor', '/Developer/Profile', 'developer', 150, true, CURRENT_TIMESTAMP)
ON CONFLICT (module_code) DO UPDATE
SET module_name = EXCLUDED.module_name,
    route_path = EXCLUDED.route_path,
    icon_name = EXCLUDED.icon_name,
    display_order = EXCLUDED.display_order,
    is_active = true;

WITH permission_seed(module_code, action_type) AS
(
    VALUES
        ('ADMIN', 'VIEW'),
        ('USER', 'VIEW'), ('USER', 'UPDATE'), ('USER', 'DISABLE'), ('USER', 'GRANT_ACCESS'),
        ('ROLE', 'VIEW'), ('ROLE', 'CREATE'), ('ROLE', 'UPDATE'), ('ROLE', 'DELETE'),
        ('PERMISSION', 'VIEW'), ('PERMISSION', 'CREATE'), ('PERMISSION', 'ASSIGN'),
        ('SHOW', 'VIEW'), ('SHOW', 'CREATE'), ('SHOW', 'UPDATE'), ('SHOW', 'DELETE'),
        ('BOOKING', 'VIEW'), ('BOOKING', 'PRINT'), ('BOOKING', 'CANCEL'),
        ('PAYMENT', 'VIEW'), ('PAYMENT', 'REFUND'),
        ('REFUND', 'VIEW'), ('REFUND', 'APPROVE'), ('REFUND', 'REJECT'), ('REFUND', 'RETRY'), ('REFUND', 'UPDATE'),
        ('WALLET', 'VIEW'), ('WALLET', 'UPDATE'),
        ('COUPON', 'VIEW'), ('COUPON', 'CREATE'), ('COUPON', 'UPDATE'), ('COUPON', 'DELETE'),
        ('NOTIFICATION', 'VIEW'), ('NOTIFICATION', 'UPDATE'),
        ('ANALYTICS', 'VIEW'),
        ('SUPPORT', 'VIEW'),
        ('SCANNER', 'VIEW'), ('SCANNER', 'VALIDATE'),
        ('DEVELOPER', 'EDIT')
)
INSERT INTO public.permissions
(
    module_id,
    permission_code,
    permission_name,
    action_type,
    description,
    created_at
)
SELECT
    am.id,
    permission_seed.module_code || '_' || permission_seed.action_type,
    replace(permission_seed.module_code || ' ' || permission_seed.action_type, '_', ' '),
    permission_seed.action_type,
    'Allows ' || lower(permission_seed.action_type) || ' access for ' || permission_seed.module_code || '.',
    CURRENT_TIMESTAMP
FROM permission_seed
JOIN public.application_modules am ON am.module_code = permission_seed.module_code
ON CONFLICT (permission_code) DO UPDATE
SET module_id = EXCLUDED.module_id,
    permission_name = EXCLUDED.permission_name,
    action_type = EXCLUDED.action_type,
    description = EXCLUDED.description;

WITH role_map(old_code, new_code) AS
(
    VALUES
        ('AMARSHOW_ADMIN_SUPER', 'AMAR_SUPER_ADMIN'),
        ('AMARSHOW_ADMIN_PLATFORM', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_SECURITY', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_BOOKING', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_PAYMENT', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_SUPPORT', 'AMAR_ADMIN'),
        ('AMARSHOW_MANAGER_CONTENT', 'AMAR_ADMIN'),
        ('AMARSHOW_MANAGER_REPORTS', 'AMAR_ADMIN'),
        ('AMARSHOW_USER_PREMIUM', 'AMAR_USER'),
        ('AMARSHOW_USER_STANDARD', 'AMAR_USER'),
        ('ADMIN', 'AMAR_ADMIN'),
        ('USER', 'AMAR_USER'),
        ('DEVELOPER', 'AMAR_DEVELOPER')
)
INSERT INTO public.user_role_mappings
(
    user_id,
    role_id,
    assigned_by,
    assigned_at,
    is_active
)
SELECT DISTINCT
    urm.user_id,
    canonical.id,
    urm.assigned_by,
    COALESCE(urm.assigned_at, CURRENT_TIMESTAMP),
    true
FROM public.user_role_mappings urm
JOIN public.roles old_role ON old_role.id = urm.role_id
JOIN role_map ON role_map.old_code = old_role.role_code
JOIN public.roles canonical ON canonical.role_code = role_map.new_code
WHERE urm.is_active = true
  AND NOT EXISTS
  (
      SELECT 1
      FROM public.user_role_mappings existing
      WHERE existing.user_id = urm.user_id
        AND existing.role_id = canonical.id
  )
ON CONFLICT DO NOTHING;

WITH role_map(old_code, new_code) AS
(
    VALUES
        ('AMARSHOW_ADMIN_SUPER', 'AMAR_SUPER_ADMIN'),
        ('AMARSHOW_ADMIN_PLATFORM', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_SECURITY', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_BOOKING', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_PAYMENT', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_SUPPORT', 'AMAR_ADMIN'),
        ('AMARSHOW_MANAGER_CONTENT', 'AMAR_ADMIN'),
        ('AMARSHOW_MANAGER_REPORTS', 'AMAR_ADMIN'),
        ('AMARSHOW_USER_PREMIUM', 'AMAR_USER'),
        ('AMARSHOW_USER_STANDARD', 'AMAR_USER'),
        ('ADMIN', 'AMAR_ADMIN'),
        ('USER', 'AMAR_USER'),
        ('DEVELOPER', 'AMAR_DEVELOPER')
),
old_active AS
(
    SELECT DISTINCT urm.user_id, canonical.id AS role_id
    FROM public.user_role_mappings urm
    JOIN public.roles old_role ON old_role.id = urm.role_id
    JOIN role_map ON role_map.old_code = old_role.role_code
    JOIN public.roles canonical ON canonical.role_code = role_map.new_code
    WHERE urm.is_active = true
)
UPDATE public.user_role_mappings existing
SET is_active = true,
    assigned_at = COALESCE(existing.assigned_at, CURRENT_TIMESTAMP)
FROM old_active
WHERE existing.user_id = old_active.user_id
  AND existing.role_id = old_active.role_id;

WITH role_map(old_code, new_code) AS
(
    VALUES
        ('AMARSHOW_ADMIN_SUPER', 'AMAR_SUPER_ADMIN'),
        ('AMARSHOW_ADMIN_PLATFORM', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_SECURITY', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_BOOKING', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_PAYMENT', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_SUPPORT', 'AMAR_ADMIN'),
        ('AMARSHOW_MANAGER_CONTENT', 'AMAR_ADMIN'),
        ('AMARSHOW_MANAGER_REPORTS', 'AMAR_ADMIN'),
        ('AMARSHOW_USER_PREMIUM', 'AMAR_USER'),
        ('AMARSHOW_USER_STANDARD', 'AMAR_USER'),
        ('ADMIN', 'AMAR_ADMIN'),
        ('USER', 'AMAR_USER'),
        ('DEVELOPER', 'AMAR_DEVELOPER')
)
INSERT INTO public.user_roles
(
    user_id,
    role_id,
    assigned_by,
    assigned_at,
    is_active
)
SELECT DISTINCT
    ur.user_id,
    canonical.id,
    ur.assigned_by,
    COALESCE(ur.assigned_at, CURRENT_TIMESTAMP),
    true
FROM public.user_roles ur
JOIN public.roles old_role ON old_role.id = ur.role_id
JOIN role_map ON role_map.old_code = old_role.role_code
JOIN public.roles canonical ON canonical.role_code = role_map.new_code
WHERE ur.is_active = true
  AND NOT EXISTS
  (
      SELECT 1
      FROM public.user_roles existing
      WHERE existing.user_id = ur.user_id
        AND existing.role_id = canonical.id
  )
ON CONFLICT DO NOTHING;

WITH role_map(old_code, new_code) AS
(
    VALUES
        ('AMARSHOW_ADMIN_SUPER', 'AMAR_SUPER_ADMIN'),
        ('AMARSHOW_ADMIN_PLATFORM', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_SECURITY', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_BOOKING', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_PAYMENT', 'AMAR_ADMIN'),
        ('AMARSHOW_ADMIN_SUPPORT', 'AMAR_ADMIN'),
        ('AMARSHOW_MANAGER_CONTENT', 'AMAR_ADMIN'),
        ('AMARSHOW_MANAGER_REPORTS', 'AMAR_ADMIN'),
        ('AMARSHOW_USER_PREMIUM', 'AMAR_USER'),
        ('AMARSHOW_USER_STANDARD', 'AMAR_USER'),
        ('ADMIN', 'AMAR_ADMIN'),
        ('USER', 'AMAR_USER'),
        ('DEVELOPER', 'AMAR_DEVELOPER')
),
old_active AS
(
    SELECT DISTINCT ur.user_id, canonical.id AS role_id
    FROM public.user_roles ur
    JOIN public.roles old_role ON old_role.id = ur.role_id
    JOIN role_map ON role_map.old_code = old_role.role_code
    JOIN public.roles canonical ON canonical.role_code = role_map.new_code
    WHERE ur.is_active = true
)
UPDATE public.user_roles existing
SET is_active = true,
    assigned_at = COALESCE(existing.assigned_at, CURRENT_TIMESTAMP)
FROM old_active
WHERE existing.user_id = old_active.user_id
  AND existing.role_id = old_active.role_id;

INSERT INTO public.user_role_mappings
(
    user_id,
    role_id,
    assigned_by,
    assigned_at,
    is_active
)
SELECT
    u.""Id"",
    r.id,
    NULL,
    CURRENT_TIMESTAMP,
    true
FROM public.""Users"" u
JOIN public.roles r ON r.role_code = 'AMAR_USER'
WHERE NOT EXISTS
(
    SELECT 1
    FROM public.user_role_mappings existing
    WHERE existing.user_id = u.""Id""
      AND existing.is_active = true
)
ON CONFLICT DO NOTHING;

UPDATE public.user_role_mappings existing
SET is_active = true,
    assigned_at = CURRENT_TIMESTAMP
FROM public.roles r
WHERE existing.role_id = r.id
  AND r.role_code = 'AMAR_USER'
  AND NOT EXISTS
  (
      SELECT 1
      FROM public.user_role_mappings active_role
      WHERE active_role.user_id = existing.user_id
        AND active_role.is_active = true
  );

INSERT INTO public.user_roles
(
    user_id,
    role_id,
    assigned_by,
    assigned_at,
    is_active
)
SELECT
    u.""Id"",
    r.id,
    NULL,
    CURRENT_TIMESTAMP,
    true
FROM public.""Users"" u
JOIN public.roles r ON r.role_code = 'AMAR_USER'
WHERE NOT EXISTS
(
    SELECT 1
    FROM public.user_roles existing
    WHERE existing.user_id = u.""Id""
      AND existing.is_active = true
)
ON CONFLICT DO NOTHING;

UPDATE public.user_roles existing
SET is_active = true,
    assigned_at = CURRENT_TIMESTAMP
FROM public.roles r
WHERE existing.role_id = r.id
  AND r.role_code = 'AMAR_USER'
  AND NOT EXISTS
  (
      SELECT 1
      FROM public.user_roles active_role
      WHERE active_role.user_id = existing.user_id
        AND active_role.is_active = true
  );

DELETE FROM public.role_permissions
WHERE true;

INSERT INTO public.role_permissions
(
    role_id,
    permission_id,
    granted_by,
    granted_at
)
SELECT r.id, p.id, NULL, CURRENT_TIMESTAMP
FROM public.roles r
JOIN public.permissions p ON true
WHERE r.role_code = 'AMAR_SUPER_ADMIN';

INSERT INTO public.role_permissions
(
    role_id,
    permission_id,
    granted_by,
    granted_at
)
SELECT r.id, p.id, NULL, CURRENT_TIMESTAMP
FROM public.roles r
JOIN public.permissions p ON p.permission_code NOT LIKE 'DEVELOPER_%'
WHERE r.role_code = 'AMAR_ADMIN';

INSERT INTO public.role_permissions
(
    role_id,
    permission_id,
    granted_by,
    granted_at
)
SELECT r.id, p.id, NULL, CURRENT_TIMESTAMP
FROM public.roles r
JOIN public.permissions p ON p.permission_code = 'DEVELOPER_EDIT'
WHERE r.role_code = 'AMAR_DEVELOPER';

INSERT INTO public.role_permissions
(
    role_id,
    permission_id,
    granted_by,
    granted_at
)
SELECT r.id, p.id, NULL, CURRENT_TIMESTAMP
FROM public.roles r
JOIN public.permissions p ON p.permission_code IN
(
    'ADMIN_VIEW',
    'USER_VIEW',
    'BOOKING_VIEW',
    'SCANNER_VIEW',
    'PAYMENT_VIEW',
    'REFUND_VIEW',
    'COUPON_VIEW',
    'WALLET_VIEW',
    'NOTIFICATION_VIEW',
    'DEVELOPER_EDIT'
)
WHERE r.role_code = 'DUM_ADMIN';

DELETE FROM public.role_menu_access
WHERE role_id IN
(
    SELECT id
    FROM public.roles
    WHERE role_code NOT IN ('AMAR_SUPER_ADMIN', 'AMAR_ADMIN', 'AMAR_DEVELOPER', 'AMAR_USER', 'DUM_ADMIN')
);

DELETE FROM public.user_role_mappings
WHERE role_id IN
(
    SELECT id
    FROM public.roles
    WHERE role_code NOT IN ('AMAR_SUPER_ADMIN', 'AMAR_ADMIN', 'AMAR_DEVELOPER', 'AMAR_USER', 'DUM_ADMIN')
);

DELETE FROM public.user_roles
WHERE role_id IN
(
    SELECT id
    FROM public.roles
    WHERE role_code NOT IN ('AMAR_SUPER_ADMIN', 'AMAR_ADMIN', 'AMAR_DEVELOPER', 'AMAR_USER', 'DUM_ADMIN')
);

DELETE FROM public.roles
WHERE role_code NOT IN ('AMAR_SUPER_ADMIN', 'AMAR_ADMIN', 'AMAR_DEVELOPER', 'AMAR_USER', 'DUM_ADMIN');

CREATE OR REPLACE VIEW public.vw_user_access_matrix AS
SELECT
    u.""Id"" AS user_id,
    u.""Name"" AS user_name,
    u.""Email"" AS user_email,
    r.role_code,
    r.role_name,
    am.module_code,
    am.module_name,
    p.permission_code,
    p.permission_name,
    p.action_type,
    urm.assigned_at,
    urm.is_active
FROM public.""Users"" u
JOIN public.user_role_mappings urm ON u.""Id"" = urm.user_id
JOIN public.roles r ON urm.role_id = r.id
JOIN public.role_permissions rp ON r.id = rp.role_id
JOIN public.permissions p ON rp.permission_id = p.id
JOIN public.application_modules am ON p.module_id = am.id
WHERE urm.is_active = true
  AND r.is_active = true
  AND am.is_active = true;

CREATE OR REPLACE VIEW public.vw_user_application_menus AS
SELECT
    u.""Id"" AS user_id,
    u.""Name"" AS user_name,
    r.role_code,
    am.id AS menu_id,
    am.module_code AS menu_code,
    am.module_name AS menu_name,
    NULL::bigint AS parent_menu_id,
    NULL::varchar(255) AS parent_menu_name,
    am.route_path,
    am.icon_name,
    1 AS menu_level,
    am.display_order,
    true AS can_view,
    bool_or(p.action_type = 'CREATE') AS can_create,
    bool_or(p.action_type = 'UPDATE') AS can_update,
    bool_or(p.action_type = 'DELETE') AS can_delete
FROM public.""Users"" u
JOIN public.user_role_mappings urm ON u.""Id"" = urm.user_id
JOIN public.roles r ON urm.role_id = r.id
JOIN public.role_permissions rp ON r.id = rp.role_id
JOIN public.permissions p ON rp.permission_id = p.id
JOIN public.application_modules am ON p.module_id = am.id
WHERE urm.is_active = true
  AND r.is_active = true
  AND am.is_active = true
GROUP BY
    u.""Id"",
    u.""Name"",
    r.role_code,
    am.id,
    am.module_code,
    am.module_name,
    am.route_path,
    am.icon_name,
    am.display_order;
");
    }
    catch(Exception ex)
    {
        app.Logger.LogWarning(
        ex,
        "RBAC schema normalization skipped"
        );
    }
}
