using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Interfaces;

public interface IAdminCredentialRepository
{
    Task<AdminCredential?> GetAsync();
    Task<AdminCredential?> GetByIdAsync(int id);
    Task<List<AdminCredential>> GetAllAsync();
    Task<AdminCredential?> GetByEmailAsync(string email);
    Task<AdminCredential?> GetByResetTokenAsync(string resetToken);
    Task<AdminCredential> AddAsync(AdminCredential credential);
    Task SaveChangesAsync();
}
