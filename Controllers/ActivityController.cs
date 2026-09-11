using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmarShowsBook.Controllers;

// Receives browser-side activity such as button clicks that never reach a normal MVC action by themselves.
public class ActivityController : Controller
{
    private readonly IActivityLogger _activityLogger;

    public ActivityController(IActivityLogger activityLogger)
    {
        _activityLogger = activityLogger;
    }

    [HttpPost]
    public async Task<IActionResult> ClientEvent([FromBody] ClientActivityEvent request)
    {
        if (request == null)
        {
            return BadRequest(new { success = false });
        }

        var userId = int.TryParse(HttpContext.Session.GetString("UserId"), out var id)
            ? id
            : (int?)null;

        // Keep client logs short; full page data is already captured by the MVC activity filter.
        await _activityLogger.LogAsync(
            userId: userId,
            action: string.Equals(request.EventType, "change", StringComparison.OrdinalIgnoreCase)
                ? "CLIENT_CHANGE"
                : "CLIENT_CLICK",
            module: "CLIENT",
            entityType: request.ElementType ?? "UI",
            description: request.Text ?? request.Href ?? request.Path ?? "Client interaction",
            status: "SUCCESS",
            metadata: request);

        return Json(new { success = true });
    }

    // This shape intentionally mirrors the tiny payload sent by both public and admin layouts.
    public class ClientActivityEvent
    {
        public string? EventType { get; set; }
        public string? ElementType { get; set; }
        public string? Text { get; set; }
        public string? Href { get; set; }
        public string? Path { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? CssClass { get; set; }
    }
}
