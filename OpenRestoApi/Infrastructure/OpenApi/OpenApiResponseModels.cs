using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Infrastructure.OpenApi;

internal sealed class AuthIdentityResponse
{
    public string? Email { get; set; }
    public string? Role { get; set; }
}

internal sealed class EmailChangeResponse
{
    public string Message { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

internal sealed class PvqVerifyResponse
{
    public string ResetToken { get; set; } = string.Empty;
}

internal sealed class UrlResponse
{
    public string Url { get; set; } = string.Empty;
}

internal sealed class BookingExtendResponse
{
    public DateTime? EndTime { get; set; }
}

internal sealed class RestaurantExtendResponse
{
    public string Message { get; set; } = string.Empty;
    public List<BookingDetailDto> ExtendedBookings { get; set; } = [];
}

internal sealed class NotificationListResponse
{
    public List<AdminNotificationDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

internal sealed class CountResponse
{
    public int Count { get; set; }
}

internal sealed class VapidPublicKeyResponse
{
    public string PublicKey { get; set; } = string.Empty;
}

internal sealed class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}

internal sealed class HealthResponse
{
    public string Status { get; set; } = string.Empty;
}
