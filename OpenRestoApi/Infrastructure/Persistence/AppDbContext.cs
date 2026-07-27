using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Restaurant> Restaurants { get; set; } = null!;
    public DbSet<Section> Sections { get; set; } = null!;
    public DbSet<Table> Tables { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;
    public DbSet<AdminCredential> AdminCredentials { get; set; } = null!;
    public DbSet<EmailSettings> EmailSettings { get; set; } = null!;
    public DbSet<BrandSettings> BrandSettings { get; set; } = null!;
    public DbSet<RestaurantHighlight> Highlights { get; set; } = null!;
    public DbSet<SocialLink> SocialLinks { get; set; } = null!;
    public DbSet<EmailFailure> EmailFailures { get; set; } = null!;
    public DbSet<AdminNotification> AdminNotifications { get; set; } = null!;
    public DbSet<AdminPushSubscription> AdminPushSubscriptions { get; set; } = null!;
    public DbSet<OperatorPrincipal> OperatorPrincipals { get; set; } = null!;
    public DbSet<OperatorRestaurantScope> OperatorRestaurantScopes { get; set; } = null!;
    public DbSet<OperatorAgentCredential> OperatorAgentCredentials { get; set; } = null!;
    public DbSet<OperatorAgentCredentialScope> OperatorAgentCredentialScopes { get; set; } = null!;
    public DbSet<OperatorActionAudit> OperatorActionAudits { get; set; } = null!;
    public DbSet<AdminCredentialManagementAudit> AdminCredentialManagementAudits { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Force UTC for all DateTime properties
        var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()),
            v => !v.HasValue ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }

        modelBuilder.Entity<Restaurant>(rb =>
        {
            rb.HasKey(r => r.Id);
            rb.Property(r => r.Name).IsRequired();
            rb.HasMany(r => r.Sections)
              .WithOne(s => s.Restaurant)
              .HasForeignKey(s => s.RestaurantId)
              .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Section>(sb =>
        {
            sb.HasKey(s => s.Id);
            sb.Property(s => s.Name).IsRequired();
            sb.HasMany(s => s.Tables)
              .WithOne(t => t.Section)
              .HasForeignKey(t => t.SectionId)
              .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Table>(tb =>
        {
            tb.HasKey(t => t.Id);
            tb.Property(t => t.Seats).IsRequired();
            tb.Property(t => t.Name);
        });

        modelBuilder.Entity<Booking>(bb =>
        {
            bb.HasKey(b => b.Id);
            bb.HasOne(b => b.Table).WithMany().HasForeignKey(b => b.TableId).OnDelete(DeleteBehavior.SetNull);
            bb.HasOne(b => b.Section).WithMany().HasForeignKey(b => b.SectionId).OnDelete(DeleteBehavior.SetNull);
            bb.HasOne(b => b.Restaurant).WithMany().HasForeignKey(b => b.RestaurantId);
            bb.HasOne(b => b.CreatedByOperator).WithMany().HasForeignKey(b => b.CreatedByOperatorId).OnDelete(DeleteBehavior.SetNull);
            bb.HasIndex(b => new { b.CreatedByOperatorId, b.RestaurantId, b.Date });
        });

        modelBuilder.Entity<AdminCredential>(a =>
        {
            a.HasKey(x => x.Id);
            a.HasIndex(x => x.Email).IsUnique();
            a.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<AdminCredentialManagementAudit>(audit =>
        {
            audit.HasKey(x => x.Id);
            audit.Property(x => x.ActorEmailSnapshot).IsRequired();
            audit.Property(x => x.TargetOperatorIdentifierSnapshot).IsRequired();
            audit.Property(x => x.ScopeRestaurantIdsSnapshot).IsRequired();
            audit.Property(x => x.ExpirationPresetSnapshot);
            audit.Property(x => x.Action).IsRequired();
            audit.HasOne(x => x.ActorAdminCredential)
                .WithMany()
                .HasForeignKey(x => x.ActorAdminCredentialId)
                .OnDelete(DeleteBehavior.SetNull);
            audit.HasOne(x => x.TargetOperatorPrincipal)
                .WithMany()
                .HasForeignKey(x => x.TargetOperatorPrincipalId)
                .OnDelete(DeleteBehavior.SetNull);
            audit.HasOne(x => x.OperatorAgentCredential)
                .WithMany()
                .HasForeignKey(x => x.OperatorAgentCredentialId)
                .OnDelete(DeleteBehavior.SetNull);
            audit.HasIndex(x => x.CreatedAtUtc);
            audit.HasIndex(x => new { x.TargetOperatorPrincipalId, x.CreatedAtUtc });
            audit.HasIndex(x => new { x.OperatorAgentCredentialId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<AdminNotification>(n =>
        {
            n.HasKey(x => x.Id);
            n.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Cascade);
            n.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.SetNull);
            n.HasIndex(x => new { x.RestaurantId, x.CreatedAt });
            n.HasIndex(x => new { x.RestaurantId, x.IsRead });
        });

        modelBuilder.Entity<AdminPushSubscription>(s =>
        {
            s.HasKey(x => x.Id);
            s.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Cascade);
            s.HasIndex(x => new { x.Endpoint, x.RestaurantId }).IsUnique();
            s.HasIndex(x => x.RestaurantId);
        });

        modelBuilder.Entity<OperatorPrincipal>(op =>
        {
            op.HasKey(x => x.Id);
            op.Property(x => x.Identifier).IsRequired();
            op.Property(x => x.NormalizedIdentifier).IsRequired();
            op.Property(x => x.IsActive).HasDefaultValue(true);
            op.HasIndex(x => x.NormalizedIdentifier).IsUnique();
        });

        modelBuilder.Entity<OperatorRestaurantScope>(scope =>
        {
            scope.HasKey(x => x.Id);
            scope.HasOne(x => x.OperatorPrincipal)
                .WithMany(x => x.RestaurantScopes)
                .HasForeignKey(x => x.OperatorPrincipalId)
                .OnDelete(DeleteBehavior.Cascade);
            scope.HasOne(x => x.Restaurant)
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
            scope.HasIndex(x => new { x.OperatorPrincipalId, x.RestaurantId }).IsUnique();
            scope.HasIndex(x => x.RestaurantId);
        });

        modelBuilder.Entity<OperatorAgentCredential>(cred =>
        {
            cred.HasKey(x => x.Id);
            cred.Property(x => x.CredentialKeyId).IsRequired();
            cred.Property(x => x.TokenDigest).IsRequired();
            cred.Property(x => x.ExpirationPreset);
            cred.HasOne(x => x.OperatorPrincipal)
                .WithMany(x => x.Credentials)
                .HasForeignKey(x => x.OperatorPrincipalId)
                .OnDelete(DeleteBehavior.Cascade);
            cred.HasIndex(x => x.CredentialKeyId).IsUnique();
            cred.HasIndex(x => new { x.ExpiresAt, x.RevokedAt });
        });

        modelBuilder.Entity<OperatorAgentCredentialScope>(scope =>
        {
            scope.HasKey(x => x.Id);
            scope.HasOne(x => x.OperatorAgentCredential)
                .WithMany(x => x.RestaurantScopes)
                .HasForeignKey(x => x.OperatorAgentCredentialId)
                .OnDelete(DeleteBehavior.Cascade);
            scope.HasOne(x => x.Restaurant)
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
            scope.HasIndex(x => new { x.OperatorAgentCredentialId, x.RestaurantId }).IsUnique();
            scope.HasIndex(x => x.RestaurantId);
        });

        modelBuilder.Entity<OperatorActionAudit>(audit =>
        {
            audit.HasKey(x => x.Id);
            audit.Property(x => x.Action).IsRequired();
            audit.Property(x => x.Outcome).IsRequired();
            audit.Property(x => x.OperatorPrincipalIdSnapshot).IsRequired();
            audit.Property(x => x.RestaurantIdSnapshot).IsRequired();
            audit.Property(x => x.RestaurantNameSnapshot).IsRequired();
            audit.HasOne(x => x.OperatorPrincipal)
                .WithMany()
                .HasForeignKey(x => x.OperatorPrincipalId)
                .OnDelete(DeleteBehavior.SetNull);
            audit.HasOne(x => x.OperatorAgentCredential)
                .WithMany()
                .HasForeignKey(x => x.OperatorAgentCredentialId)
                .OnDelete(DeleteBehavior.SetNull);
            audit.HasOne(x => x.Restaurant)
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.SetNull);
            audit.HasOne(x => x.Booking)
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
            audit.HasIndex(x => new { x.OperatorPrincipalId, x.CreatedAt });
            audit.HasIndex(x => new { x.RestaurantId, x.CreatedAt });
            audit.HasIndex(x => new { x.BookingId, x.CreatedAt });
        });
    }
}
