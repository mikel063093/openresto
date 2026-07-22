namespace OpenRestoApi.Core.Application.Interfaces;

public interface IAuthService
{
    Task<string?> LoginAsync(string email, string password);
    Task<bool> ChangePasswordAsync(string email, string currentPassword, string newPassword);
    Task<string?> ChangeEmailAsync(string email, string currentPassword, string newEmail);
    Task<bool> ResetPasswordAsync(string resetToken, string newPassword);
}
