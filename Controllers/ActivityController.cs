using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmarShowsBook.Controllers;

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
        var userId = int.TryParse(HttpContext.Session.GetString("UserId"), out var id)
            ? id
            : (int?)null;

        await _activityLogger.LogAsync(
            userId: userId,
            action: "CLIENT_CLICK",
            module: "CLIENT",
            entityType: request.ElementType ?? "UI",
            description: request.Text ?? request.Href ?? request.Path ?? "Client interaction",
            status: "SUCCESS",
            metadata: request);

        return Json(new { success = true });
    }

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
