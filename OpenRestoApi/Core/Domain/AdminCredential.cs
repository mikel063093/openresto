namespace OpenRestoApi.Core.Domain;

public class AdminCredential
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;
    public AdminRole Role { get; set; } = AdminRole.SuperAdmin;
    public bool IsActive { get; set; } = true;
    public string? PvqQuestion { get; set; }
    public string? PvqAnswerHash { get; set; }
    public string? PvqAnswerSalt { get; set; }
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
}
