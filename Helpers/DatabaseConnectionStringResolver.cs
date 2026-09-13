using Npgsql;

namespace AmarShowsBook.Helpers;

public static class DatabaseConnectionStringResolver
{
    public static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        var databaseUrl =
        Environment.GetEnvironmentVariable("DATABASE_URL");

        if(!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return NormalizePostgresConnection(databaseUrl);
        }

        return configuration.GetConnectionString(
        "DefaultConnection")
        ?? "";
    }

    private static string NormalizePostgresConnection(string value)
    {
        var trimmed =
        value.Trim();

        if(trimmed.Contains('='))
        {
            var builder =
            new NpgsqlConnectionStringBuilder(trimmed);

            if(builder.SslMode == SslMode.Disable)
            {
                builder.SslMode =
                SslMode.Require;
            }

            return builder.ConnectionString;
        }

        if(!Uri.TryCreate(
        trimmed,
        UriKind.Absolute,
        out var uri)
        || (
        uri.Scheme != "postgres"
        && uri.Scheme != "postgresql"))
        {
            throw new InvalidOperationException(
            "DATABASE_URL must be a full PostgreSQL URL like 'postgresql://user:password@host:5432/database' or an Npgsql connection string like 'Host=...;Port=5432;Database=...;Username=...;Password=...'. Do not set DATABASE_URL to only the Render hostname.");
        }

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

        var connectionBuilder =
        new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = database,
            Username = username,
            Password = password,
            SslMode = SslMode.Require
        };

        return connectionBuilder.ConnectionString;
    }
}
