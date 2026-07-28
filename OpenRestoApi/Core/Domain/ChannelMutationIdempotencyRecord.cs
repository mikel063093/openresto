namespace OpenRestoApi.Core.Domain;

public sealed class ChannelMutationIdempotencyRecord
{
    public int Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string MutationScope { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ReplayKey { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
