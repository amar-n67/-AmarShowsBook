namespace AmarShowsBook.Services
{
    using AmarShowsBook.Helpers;
    using Microsoft.AspNetCore.Http;
    using Npgsql;
    using NpgsqlTypes;
    using System.Text.Json;

    // Writes activity rows directly with Npgsql so logging remains usable even when EF mappings are changing.
    public class ActivityLogger : IActivityLogger
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ActivityLogger> _logger;

        public ActivityLogger(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ActivityLogger> logger)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Controllers pass business context here; the method adds request context before saving the row.
        public async Task LogAsync(
            int? userId,
            string action,
            string module,
            string entityType,
            int? entityId = null,
            string? description = null,
            object? oldValue = null,
            object? newValue = null,
            string status = "SUCCESS",
            string? errorCode = null,
            string? errorMessage = null,
            string? errorSource = null,
            string? stackTrace = null,
            int isError = 0,
            object? metadata = null)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;

                // These request fields make a plain activity row useful during admin investigation.
                var requestMethod = context?.Request?.Method;
                var endpoint = context?.Request?.Path.ToString();
                var ipAddress = context?.Connection?.RemoteIpAddress?.ToString();
                var userAgent = context?.Request?.Headers["User-Agent"].ToString();

                var connectionString =
                    DatabaseConnectionStringResolver.GetDatabaseConnectionString(_configuration);

                await using var connection = new NpgsqlConnection(connectionString);

                await connection.OpenAsync();

                const string query = @"
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

                AddValue(command, "@user_id", userId.HasValue ? userId.Value : DBNull.Value);
                AddValue(command, "@action", action);
                AddValue(command, "@module", module);
                AddValue(command, "@entity_type", entityType ?? (object)DBNull.Value);
                AddValue(command, "@entity_id", entityId.HasValue ? entityId.Value : DBNull.Value);
                AddValue(command, "@description", description ?? (object)DBNull.Value);
                AddValue(command, "@request_method", requestMethod ?? (object)DBNull.Value);
                AddValue(command, "@endpoint", endpoint ?? (object)DBNull.Value);
                AddValue(command, "@ip_address", ipAddress ?? (object)DBNull.Value);
                AddValue(command, "@user_agent", userAgent ?? (object)DBNull.Value);
                AddValue(command, "@status", status);
                AddValue(command, "@error_code", errorCode ?? (object)DBNull.Value);
                AddValue(command, "@error_message", errorMessage ?? (object)DBNull.Value);
                AddValue(command, "@error_source", errorSource ?? (object)DBNull.Value);
                AddValue(command, "@stack_trace", stackTrace ?? (object)DBNull.Value);
                AddValue(command, "@is_error", isError);

                AddJsonValue(command, "@old_value", oldValue);
                AddJsonValue(command, "@new_value", newValue);
                AddJsonValue(command, "@metadata", metadata);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Audit logging is best effort. A logging failure should never block login, booking, or admin work.
                _logger.LogWarning(ex, "Activity log skipped for {Module}/{Action}.", module, action);
            }
        }

        private static void AddValue(NpgsqlCommand command, string parameterName, object value)
        {
            command.Parameters.AddWithValue(parameterName, value);
        }

        private static void AddJsonValue(NpgsqlCommand command, string parameterName, object? value)
        {
            command.Parameters.AddWithValue(
                parameterName,
                NpgsqlDbType.Jsonb,
                value != null
                    ? JsonSerializer.Serialize(value)
                    : "{}");
        }
    }
}
