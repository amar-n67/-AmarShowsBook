namespace AmarShowsBook.Services
{
using AmarShowsBook.Helpers;
using NpgsqlTypes;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Npgsql;

public class ActivityLogger : IActivityLogger
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityLogger(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    // Every controller can call this without caring whether the action succeeded, failed, or changed data.
     public async Task LogAsync(
    int? userId,
    string action,
    string module,
    string entityType,
    int? entityId = null,
    string description = null,
    object oldValue = null,
    object newValue = null,
    string status = "SUCCESS",

    string? errorCode = null,
    string? errorMessage = null,
    string? errorSource = null,
    string? stackTrace = null,
    int isError = 0,

    object metadata = null
)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;

            // The request snapshot makes activity logs useful from the admin audit page.
            var requestMethod = context?.Request?.Method;
           var endpoint = context?.Request?.Path.ToString();
            var ipAddress = context?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = context?.Request?.Headers["User-Agent"].ToString();

            var connectionString =
                DatabaseConnectionStringResolver
                .GetDatabaseConnectionString(_configuration);

            await using var connection = new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

                
var query = @"
INSERT INTO activity_logs
(
    user_id,
    action,
    module,
    entity_type,
    entity_id,
    description,
    request_method,
    endpoint,
    ip_address,
    user_agent,
    status,
error_code,
error_message,
error_source,
stack_trace,
is_error,
old_value,
new_value,
metadata
)
VALUES
(
    @user_id,
    @action,
    @module,
    @entity_type,
    @entity_id,
    @description,
    @request_method,
    @endpoint,
    @ip_address,
    @user_agent,
    @status,
@error_code,
@error_message,
@error_source,
@stack_trace,
@is_error,
@old_value,
@new_value,
@metadata
);
";
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue(
    "@user_id",
    userId.HasValue ? userId.Value : DBNull.Value
);

            command.Parameters.AddWithValue("@action", action);

            command.Parameters.AddWithValue("@module", module);

            command.Parameters.AddWithValue(
    "@entity_type",
    entityType ?? (object)DBNull.Value
);

            command.Parameters.AddWithValue(
    "@entity_id",
    entityId.HasValue ? entityId.Value : DBNull.Value
);

            command.Parameters.AddWithValue("@description",
                (object?)description ?? DBNull.Value);

            command.Parameters.AddWithValue("@request_method",
                (object?)requestMethod ?? DBNull.Value);

            command.Parameters.AddWithValue("@endpoint",
                (object?)endpoint ?? DBNull.Value);

            command.Parameters.AddWithValue("@ip_address",
                (object?)ipAddress ?? DBNull.Value);

            command.Parameters.AddWithValue("@user_agent",
                (object?)userAgent ?? DBNull.Value);

            command.Parameters.AddWithValue("@status", status);
command.Parameters.AddWithValue("@error_code",
    (object?)errorCode ?? DBNull.Value);

command.Parameters.AddWithValue("@error_message",
    (object?)errorMessage ?? DBNull.Value);

command.Parameters.AddWithValue("@error_source",
    (object?)errorSource ?? DBNull.Value);

command.Parameters.AddWithValue("@stack_trace",
    (object?)stackTrace ?? DBNull.Value);

command.Parameters.AddWithValue("@is_error", isError);


command.Parameters.AddWithValue(
    "@old_value",
    NpgsqlTypes.NpgsqlDbType.Jsonb,
    oldValue != null
        ? JsonSerializer.Serialize(oldValue)
        : "{}"
);

command.Parameters.AddWithValue(
    "@new_value",
    NpgsqlTypes.NpgsqlDbType.Jsonb,
    newValue != null
        ? JsonSerializer.Serialize(newValue)
        : "{}"
);

command.Parameters.AddWithValue(
    "@metadata",
    NpgsqlTypes.NpgsqlDbType.Jsonb,
    metadata != null
        ? JsonSerializer.Serialize(metadata)
        : "{}"
);

await command.ExecuteNonQueryAsync();
        }
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"Activity log skipped: {ex.Message}");
}
    }
}
}
