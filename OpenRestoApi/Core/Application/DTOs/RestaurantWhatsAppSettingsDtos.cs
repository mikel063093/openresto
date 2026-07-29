namespace OpenRestoApi.Core.Application.DTOs;

public sealed class UpdateRestaurantWhatsAppSettingsRequestDto
{
    public bool IsWhatsAppTestEnabled { get; set; }
    public string? HandoffWhatsAppE164 { get; set; }
}

public sealed class RestaurantWhatsAppSettingsDto
{
    public int RestaurantId { get; set; }
    public bool IsWhatsAppTestEnabled { get; set; }
    public string? HandoffWhatsAppE164 { get; set; }
}
