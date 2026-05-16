namespace AmarShowsBook.Services
{
    public interface IActivityLogger
    {
      Task LogAsync(
    int? userId = null,
    string action = "",
    string module = "",
    string entityType = "",
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
);
    }
}