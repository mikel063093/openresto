using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;
using OpenRestoApi.Infrastructure.Persistence.Repositories;

namespace OpenRestoApi.Tests.Services;

public class AdminUserServiceTests
{
    private static AdminUserService CreateService(AppDbContext db) =>
        new(new AdminCredentialRepository(db), new PasswordService());

    private static void Seed(AppDbContext db, string email, string password, AdminRole role = AdminRole.SuperAdmin, bool isActive = true)
    {
        var passwords = new PasswordService();
        (string hash, string salt) = passwords.Hash(password);
        db.AdminCredentials.Add(new AdminCredential
        {
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            IsActive = isActive,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_Adds_Active_User_With_Normalized_Unique_Email()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(CreateAsync_Adds_Active_User_With_Normalized_Unique_Email));
        AdminUserService service = CreateService(db);

        AdminUserDto user = await service.CreateAsync(new CreateAdminUserRequest
        {
            Email = "  Viewer@Example.com ", Password = "password", Role = AdminRole.BookingViewer
        });

        Assert.Equal("viewer@example.com", user.Email);
        Assert.True(user.IsActive);
        Assert.Equal(AdminRole.BookingViewer, user.Role);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateAdminUserRequest
        {
            Email = "VIEWER@example.com", Password = "password", Role = AdminRole.BookingViewer
        }));
    }

    [Fact]
    public async Task UpdateAsync_Refuses_To_Demote_Only_Active_SuperAdmin()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(UpdateAsync_Refuses_To_Demote_Only_Active_SuperAdmin));
        Seed(db, "admin@example.com", "password");
        AdminCredential admin = await db.AdminCredentials.SingleAsync();
        AdminUserService service = CreateService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(admin.Id, new UpdateAdminUserRequest
        {
            Role = AdminRole.BookingEditor
        }));
    }

    [Fact]
    public async Task DeactivateAsync_Refuses_To_Deactivate_Only_Active_SuperAdmin()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(DeactivateAsync_Refuses_To_Deactivate_Only_Active_SuperAdmin));
        Seed(db, "admin@example.com", "password");
        AdminCredential admin = await db.AdminCredentials.SingleAsync();
        AdminUserService service = CreateService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeactivateAsync(admin.Id));
    }

    [Fact]
    public async Task DeactivateAsync_Allows_Deactivating_A_SuperAdmin_When_Another_Is_Active()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(DeactivateAsync_Allows_Deactivating_A_SuperAdmin_When_Another_Is_Active));
        Seed(db, "first@example.com", "password");
        Seed(db, "second@example.com", "password");
        AdminCredential first = await db.AdminCredentials.OrderBy(x => x.Id).FirstAsync();
        AdminUserService service = CreateService(db);

        await service.DeactivateAsync(first.Id);

        Assert.False((await db.AdminCredentials.FindAsync(first.Id))!.IsActive);
    }
}
