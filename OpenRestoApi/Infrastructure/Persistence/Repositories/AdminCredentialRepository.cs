using CustomAccessibility.Attributes;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Infrastructure.Persistence.Repositories;

[OnlyAccessibleBy("OpenRestoApi.Extensions.ServiceCollectionExtensions")]
[OnlyAccessibleBy("OpenRestoApi.Tests.Services.AuthServiceTests")]
[OnlyAccessibleBy("OpenRestoApi.Tests.Services.AdminUserServiceTests")]
[OnlyAccessibleBy("OpenRestoApi.Tests.Services.SecurityQuestionsServiceTests")]
[ExternalAccessAllowed]
internal class AdminCredentialRepository(AppDbContext db) : IAdminCredentialRepository
{
    private readonly AppDbContext _db = db;

    public async Task<AdminCredential?> GetAsync() => await _db.AdminCredentials.FirstOrDefaultAsync();

    public async Task<AdminCredential?> GetByIdAsync(int id) => await _db.AdminCredentials.FindAsync(id);

    public async Task<List<AdminCredential>> GetAllAsync() => await _db.AdminCredentials.OrderBy(c => c.Email).ToListAsync();

    public async Task<AdminCredential?> GetByEmailAsync(string email)
    {
        string normalized = email.Trim().ToLowerInvariant();
#pragma warning disable CA1862, CA1311, CA1304
        return await _db.AdminCredentials.FirstOrDefaultAsync(c => c.Email.ToLower() == normalized);
#pragma warning restore CA1862, CA1311, CA1304
    }

    public async Task<AdminCredential?> GetByResetTokenAsync(string resetToken) =>
        await _db.AdminCredentials.FirstOrDefaultAsync(c => c.ResetToken == resetToken);

    public async Task<AdminCredential> AddAsync(AdminCredential credential)
    {
        _db.AdminCredentials.Add(credential);
        await _db.SaveChangesAsync();
        return credential;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
