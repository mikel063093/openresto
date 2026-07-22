using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Application.Utilities;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Services;

public class AdminUserService(IAdminCredentialRepository credentials, IPasswordService passwords)
{
    private readonly IAdminCredentialRepository _credentials = credentials;
    private readonly IPasswordService _passwords = passwords;

    public async Task<List<AdminUserDto>> GetAllAsync() => (await _credentials.GetAllAsync()).Select(ToDto).ToList();

    public async Task<AdminUserDto> CreateAsync(CreateAdminUserRequest request)
    {
        string email = NormalizeAndValidateEmail(request.Email);
        ValidatePassword(request.Password);
        if (await _credentials.GetByEmailAsync(email) != null)
            throw new BusinessRuleException("An account with that email already exists.");
        (string hash, string salt) = _passwords.Hash(request.Password);
        AdminCredential credential = await _credentials.AddAsync(new AdminCredential
        {
            Email = email, PasswordHash = hash, PasswordSalt = salt, Role = request.Role, IsActive = true
        });
        return ToDto(credential);
    }

    public async Task<AdminUserDto> UpdateAsync(int id, UpdateAdminUserRequest request)
    {
        AdminCredential credential = await _credentials.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
        bool removesSuperAdmin = credential.IsActive && credential.Role == AdminRole.SuperAdmin &&
            ((request.Role.HasValue && request.Role != AdminRole.SuperAdmin) || request.IsActive == false);
        if (removesSuperAdmin) await EnsureAnotherActiveSuperAdminAsync(id);
        if (request.Email != null)
        {
            string email = NormalizeAndValidateEmail(request.Email);
            AdminCredential? existing = await _credentials.GetByEmailAsync(email);
            if (existing != null && existing.Id != id) throw new BusinessRuleException("An account with that email already exists.");
            credential.Email = email;
        }
        if (request.Password != null)
        {
            ValidatePassword(request.Password);
            (credential.PasswordHash, credential.PasswordSalt) = _passwords.Hash(request.Password);
        }
        if (request.Role.HasValue) credential.Role = request.Role.Value;
        if (request.IsActive.HasValue) credential.IsActive = request.IsActive.Value;
        await _credentials.SaveChangesAsync();
        return ToDto(credential);
    }

    public async Task DeactivateAsync(int id)
    {
        AdminCredential credential = await _credentials.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
        if (!credential.IsActive) return;
        if (credential.Role == AdminRole.SuperAdmin) await EnsureAnotherActiveSuperAdminAsync(id);
        credential.IsActive = false;
        await _credentials.SaveChangesAsync();
    }

    private async Task EnsureAnotherActiveSuperAdminAsync(int excludedId)
    {
        bool hasAnother = (await _credentials.GetAllAsync()).Any(user => user.Id != excludedId && user.IsActive && user.Role == AdminRole.SuperAdmin);
        if (!hasAnother) throw new BusinessRuleException("At least one active SuperAdmin is required.");
    }

    private static string NormalizeAndValidateEmail(string email)
    {
        if (!EmailValidator.IsValid(email)) throw new ValidationException("A valid email address is required.");
        return email.Trim().ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 6)
            throw new ValidationException("Password must be at least 6 characters.");
    }

    private static AdminUserDto ToDto(AdminCredential credential) => new()
    {
        Id = credential.Id, Email = credential.Email, Role = credential.Role, IsActive = credential.IsActive
    };
}
