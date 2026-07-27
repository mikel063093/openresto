using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Infrastructure.OpenApi;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    private readonly INotificationService _notifications = notificationService;

    /// <summary>
    /// List notification history with optional filters, newest first.
    /// GET /api/admin/notifications?restaurantId=1&amp;type=BookingCreated&amp;unreadOnly=true&amp;page=1&amp;pageSize=20
    /// restaurantId is optional — omit to see all restaurants.
    /// type values: BookingCreated | BookingCancelled | RestaurantNearlyFull
    /// </summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(NotificationListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int? restaurantId,
        [FromQuery] string? type,
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        (List<AdminNotificationDto> items, int total) = await _notifications.GetNotificationsAsync(
            restaurantId, type, unreadOnly, page, pageSize);

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    /// <summary>
    /// Unread count badge. Omit restaurantId to get total across all restaurants.
    /// GET /api/admin/notifications/unread-count
    /// GET /api/admin/notifications/unread-count?restaurantId=1
    /// </summary>
    [HttpGet("notifications/unread-count")]
    [ProducesResponseType(typeof(CountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount([FromQuery] int? restaurantId)
    {
        int count = await _notifications.GetUnreadCountAsync(restaurantId);
        return Ok(new { count });
    }

    /// <summary>
    /// Mark a single notification as read.
    /// PATCH /api/admin/notifications/{id}/read
    /// </summary>
    [HttpPatch("notifications/{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _notifications.MarkReadAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Mark all notifications for a restaurant as read.
    /// PATCH /api/admin/notifications/read-all?restaurantId=1
    /// </summary>
    [HttpPatch("notifications/read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkAllRead([FromQuery] int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest(new { error = "restaurantId is required." });

        await _notifications.MarkAllReadAsync(restaurantId);
        return NoContent();
    }

    /// <summary>
    /// Returns the VAPID public key for the frontend to use when subscribing.
    /// GET /api/admin/push/vapid-public-key
    /// Returns 204 if VAPID is not configured (push disabled).
    /// </summary>
    [HttpGet("push/vapid-public-key")]
    [ProducesResponseType(typeof(VapidPublicKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetVapidPublicKey()
    {
        string? key = _notifications.GetVapidPublicKey();
        if (key is null) return NoContent();
        return Ok(new { publicKey = key });
    }

    /// <summary>
    /// Register a browser push subscription.
    /// POST /api/admin/push/subscribe?restaurantId=1
    /// Body: { endpoint, p256dh, auth, userAgent? }
    /// </summary>
    [HttpPost("push/subscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe(
        [FromQuery] int restaurantId,
        [FromBody] PushSubscribeRequest request)
    {
        if (restaurantId <= 0)
            return BadRequest(new { error = "restaurantId is required." });

        await _notifications.SubscribeAsync(restaurantId, request);
        return Ok();
    }

    /// <summary>
    /// Remove a push subscription (logout / permission revoked).
    /// DELETE /api/admin/push/subscribe
    /// Body: "https://fcm.googleapis.com/..."
    /// </summary>
    [HttpDelete("push/subscribe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unsubscribe([FromBody] string endpoint)
    {
        await _notifications.UnsubscribeAsync(endpoint);
        return NoContent();
    }

    [HttpDelete("notifications/all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAll(
        [FromQuery] int? restaurantId,
        [FromQuery] string? type,
        [FromQuery] bool? unreadOnly)
    {
        await _notifications.DeleteAllAsync(restaurantId, type, unreadOnly);
        return NoContent();
    }

    [HttpDelete("notifications/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        await _notifications.DeleteByIdAsync(id);
        return NoContent();
    }

    [HttpDelete("notifications")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteNotifications([FromBody] List<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return BadRequest(new { error = "List of notification IDs is required." });

        await _notifications.DeleteByIdsAsync(ids);
        return NoContent();
    }
}
