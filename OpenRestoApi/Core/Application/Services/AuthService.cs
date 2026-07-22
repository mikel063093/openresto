using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Application.Utilities;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Services;

public class AuthService(
    IAdminCredentialRepository credentialRepository,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService,
    IConfiguration config) : IAuthService
{
    private readonly IAdminCredentialRepository _credentialRepository = credentialRepository;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly IConfiguration _config = config;

    public virtual async Task<string?> LoginAsync(string email, string password)
    {
        AdminCredential? credential = await _credentialRepository.GetByEmailAsync(email.Trim().ToLowerInvariant());
        // Bootstrap exactly once. After accounts exist, a former bootstrap email
        // must never recreate a privileged account.
        if (credential == null && !(await _credentialRepository.GetAllAsync()).Any())
            credential = await GetOrCreateCredentialAsync();
        if (credential == null || !credential.IsActive || !CredentialHelper.VerifyPassword(credential, password, _passwordService))
            return null;
        return _jwtTokenService.Generate(credential.Email, credential.Role);
    }

    public virtual async Task<bool> ChangePasswordAsync(string email, string currentPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            throw new ValidationException("Password must be at least 6 characters.");
        AdminCredential? credential = await _credentialRepository.GetByEmailAsync(email);
        if (credential == null || !credential.IsActive || !CredentialHelper.VerifyPassword(credential, currentPassword, _passwordService))
            return false;
        (credential.PasswordHash, credential.PasswordSalt) = _passwordService.Hash(newPassword);
        await _credentialRepository.SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        AdminCredential? credential = await _credentialRepository.GetAsync();
        return credential != null && await ChangePasswordAsync(credential.Email, currentPassword, newPassword);
    }

    public virtual async Task<string?> ChangeEmailAsync(string email, string currentPassword, string newEmail)
    {
        if (!EmailValidator.IsValid(newEmail))
            throw new ValidationException("A valid email address is required.");
        AdminCredential? credential = await _credentialRepository.GetByEmailAsync(email);
        if (credential == null || !credential.IsActive || !CredentialHelper.VerifyPassword(credential, currentPassword, _passwordService))
            return null;
        string normalizedEmail = newEmail.Trim().ToLowerInvariant();
        if (string.Equals(normalizedEmail, credential.Email, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("New email must be different from the current email.");
        if (await _credentialRepository.GetByEmailAsync(normalizedEmail) != null)
            throw new BusinessRuleException("An account with that email already exists.");
        credential.Email = normalizedEmail;
        await _credentialRepository.SaveChangesAsync();
        return _jwtTokenService.Generate(credential.Email, credential.Role);
    }

    public virtual async Task<string?> ChangeEmailAsync(string currentPassword, string newEmail)
    {
        AdminCredential? credential = await _credentialRepository.GetAsync();
        return credential == null ? null : await ChangeEmailAsync(credential.Email, currentPassword, newEmail);
    }

    public virtual async Task<bool> ResetPasswordAsync(string resetToken, string newPassword)
    {
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            throw new ValidationException("Password must be at least 6 characters.");
        AdminCredential? credential = await _credentialRepository.GetByResetTokenAsync(resetToken);
        if (credential == null || !credential.IsActive || credential.ResetTokenExpiry < DateTime.UtcNow)
            return false;
        (credential.PasswordHash, credential.PasswordSalt) = _passwordService.Hash(newPassword);
        credential.ResetToken = null;
        credential.ResetTokenExpiry = null;
        await _credentialRepository.SaveChangesAsync();
        return true;
    }

    private string GetConfiguredAdminEmail()
    {
        string? configEmail = _config["Admin:Email"];
        string email = !string.IsNullOrWhiteSpace(configEmail)
            ? configEmail : Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@openresto.com";
        return email.Trim().ToLowerInvariant();
    }

    private async Task<AdminCredential> GetOrCreateCredentialAsync()
    {
        string email = GetConfiguredAdminEmail();
        AdminCredential? existing = await _credentialRepository.GetByEmailAsync(email);
        if (existing != null) return existing;
        string? configPassword = _config["Admin:Password"];
        string? password = !string.IsNullOrWhiteSpace(configPassword)
            ? configPassword : Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
            throw new InfrastructureException("Admin:Password must be configured before first use. Set it via ADMIN_PASSWORD env var.");
        (string hash, string salt) = _passwordService.Hash(password);
        return await _credentialRepository.AddAsync(new AdminCredential
        {
            Email = email.Trim().ToLowerInvariant(), PasswordHash = hash, PasswordSalt = salt,
            Role = AdminRole.SuperAdmin, IsActive = true
        });
    }
}
