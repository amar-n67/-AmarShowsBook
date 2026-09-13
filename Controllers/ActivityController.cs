using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmarShowsBook.Controllers;

// Receives browser-side activity such as button clicks that never reach a normal MVC action by themselves.
public class ActivityController : Controller
{
    private readonly IActivityLogger _activityLogger;

    public ActivityController(IActivityLogger activityLogger)
    {
        // Browser-side UI events use the same database activity table as server-side MVC actions.
        _activityLogger = activityLogger;
    }

    [HttpPost]
    // Saves click/change activity sent from shared layouts after the user is logged in.
    public async Task<IActionResult> ClientEvent([FromBody] ClientActivityEvent request)
    {
        if (request == null)
        {
            // Empty payloads are rejected because they do not describe a real UI interaction.
            return BadRequest(new { success = false });
        }

        var userId = int.TryParse(HttpContext.Session.GetString("UserId"), out var id)
            ? id
            : (int?)null;
        var eventType = NormalizeEventType(request.EventType);
        var description = FirstFilled(request.Text, request.Href, request.Path, "Client interaction");

        // Keep client logs short; full page data is already captured by the MVC activity filter.
        await _activityLogger.LogAsync(
            userId: userId,
            action: string.Equals(eventType, "change", StringComparison.OrdinalIgnoreCase)
                ? "CLIENT_CHANGE"
                : "CLIENT_CLICK",
            module: "CLIENT",
            entityType: request.ElementType ?? "UI",
            description: description,
            status: "SUCCESS",
            metadata: new
            {
                eventType,
                request.ElementType,
                request.InputType,
                Text = SafeText(request.Text),
                Href = SafeText(request.Href),
                Path = SafeText(request.Path),
                Id = SafeText(request.Id),
                Name = SafeText(request.Name),
                CssClass = SafeText(request.CssClass)
            });

        return Json(new { success = true });
    }

    // Only click and change are supported; anything else is normalized to a click.
    private static string NormalizeEventType(string? eventType)
    {
        return string.Equals(eventType, "change", StringComparison.OrdinalIgnoreCase)
            ? "change"
            : "click";
    }

    // Picks the first useful label for the activity description.
    private static string FirstFilled(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";
    }

    // Keeps UI metadata short before it is stored as JSON.
    private static string? SafeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed[..Math.Min(trimmed.Length, 250)];
    }

    // This shape intentionally mirrors the tiny payload sent by both public and admin layouts.
    public class ClientActivityEvent
    {
        public string? EventType { get; set; }
        public string? ElementType { get; set; }
        public string? InputType { get; set; }
        public string? Text { get; set; }
        public string? Href { get; set; }
        public string? Path { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? CssClass { get; set; }
    }
}
