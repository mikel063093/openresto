using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.DTOs;

// ── Auth ─────────────────────────────────────────────────────────────────────

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

// ── Overview / Stats ─────────────────────────────────────────────────────────

public class AdminOverviewDto
{
    public int TotalRestaurants { get; set; }
    public int TotalBookings { get; set; }
    public int TodayBookings { get; set; }
    public int TotalSeats { get; set; }
    public int ActiveHoldsCount { get; set; }
    public int PausedRestaurantsCount { get; set; }
    public List<int> OccupancyData { get; set; } = [];
    public List<string> OccupancyDates { get; set; } = [];
    public List<int> OccupancyCounts { get; set; } = [];
    public List<BookingDetailDto> TodayBookingsList { get; set; } = [];
}

// ── Booking detail (admin view — includes resolved names) ────────────────────

public class BookingDetailDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public int? SectionId { get; set; }
    public string? SectionName { get; set; }
    public int? TableId { get; set; }
    public string? TableName { get; set; }
    public DateTime Date { get; set; }
    public DateTime? EndTime { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public int Seats { get; set; }
    public string? SpecialRequests { get; set; }
    public string? BookingRef { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public class ExtendBookingRequest
{
    /// <summary>Additional minutes to add to the booking's EndTime.</summary>
    public int Minutes { get; set; }
}

// ── Send email to booking guest ──────────────────────────────────────────────

public class SendBookingEmailRequest
{
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}

// ── Admin booking create (walk-in — no hold required) ────────────────────────

public class AdminCreateBookingRequest
{
    public int RestaurantId { get; set; }
    public int SectionId { get; set; }
    public int TableId { get; set; }
    public DateTime Date { get; set; }
    public string CustomerEmail { get; set; } = null!;
    public string? CustomerName { get; set; }
    public int Seats { get; set; }
}

// ── Admin booking update (PUT — can modify all fields) ───────────────────────

public class AdminUpdateBookingRequest
{
    public int? RestaurantId { get; set; }
    public int? SectionId { get; set; }
    public int? TableId { get; set; }
    public DateTime? Date { get; set; }
    public int? Seats { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? SpecialRequests { get; set; }
}

// ── Admin restaurant create ───────────────────────────────────────────────────

public class CreateRestaurantRequest
{
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}

// ── PVQ (Personal Verification Questions) ────────────────────────────────────

public class SetupPvqRequest
{
    public string Question { get; set; } = null!;
    public string Answer { get; set; } = null!;
}

public class VerifyPvqRequest
{
    public string Email { get; set; } = null!;
    public string Answer { get; set; } = null!;
}

public class ResetPasswordRequest
{
    public string ResetToken { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ChangeEmailRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewEmail { get; set; } = null!;
}

public class LookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime? BookingsPausedUntil { get; set; }
    public int ActiveBookingsCount { get; set; }
    public bool IsArchived { get; set; }
}

public class AdminRestaurantPatchRequest
{
    public bool? IsArchived { get; set; }
}

public class MessageResponse
{
    public string Message { get; set; } = null!;
}

public class PvqStatusDto
{
    public bool IsConfigured { get; set; }
    public string? Question { get; set; }
}

public class AdminUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public AdminRole Role { get; set; }
    public bool IsActive { get; set; }
}

public class CreateAdminUserRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public AdminRole Role { get; set; }
}

public class UpdateAdminUserRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public AdminRole? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class IssueOperatorCredentialRequestDto
{
    public string Identifier { get; set; } = null!;
    public List<int> RestaurantIds { get; set; } = [];
    public int? TtlHours { get; set; }
    public string? Notes { get; set; }
}

public class OperatorCredentialScopeDto
{
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = null!;
}

public class OperatorCredentialListItemDto
{
    public int CredentialId { get; set; }
    public string Identifier { get; set; } = null!;
    public string CredentialKeyId { get; set; } = null!;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public string? Notes { get; set; }
    public List<OperatorCredentialScopeDto> Restaurants { get; set; } = [];
}

public class IssueOperatorCredentialResponseDto : OperatorCredentialListItemDto
{
    public string PlaintextToken { get; set; } = null!;
}
