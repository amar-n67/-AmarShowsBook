namespace AmarShowsBook.Services
{
    // Shared audit writer used by controllers, filters, and lightweight client-event endpoints.
    // Keep this interface broad enough for business events and failures without leaking database details.
    public interface IActivityLogger
    {
        Task LogAsync(
            int? userId = null,
            string action = "",
            string module = "",
            string entityType = "",
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

            object? metadata = null
        );
    }
}
