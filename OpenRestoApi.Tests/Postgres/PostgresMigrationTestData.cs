using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Postgres;

internal static class PostgresMigrationTestData
{
    public const int AdminCredentialId = 9;
    public const int BrandSettingsId = 3;
    public const int ChannelMutationIdempotencyRecordId = 11;
    public const int EmailFailureId = 12;
    public const int EmailSettingsId = 13;
    public const int HighlightId = 14;
    public const int RestaurantId = 15;
    public const int SocialLinkId = 16;
    public const int OperatorPrincipalId = 21;
    public const int SectionId = 32;
    public const int TableId = 48;
    public const int AdminPushSubscriptionId = 49;
    public const int OperatorAgentCredentialId = 50;
    public const int OperatorRestaurantScopeId = 51;
    public const int RestaurantOccasionCatalogItemId = 52;
    public const int OperatorAgentCredentialScopeId = 53;
    public const int AdminCredentialManagementAuditId = 54;
    public const int AdminNotificationId = 55;
    public const int OperatorActionAuditId = 56;
    public const int BookingOccasionSnapshotId = 57;
    public const int WhatsAppHandoffAuditId = 58;
    public const int BookingId = 77;

    public static IReadOnlyList<string> ImportedEntityNames { get; } =
    [
        nameof(AdminCredential),
        nameof(BrandSettings),
        nameof(ChannelMutationIdempotencyRecord),
        nameof(EmailFailure),
        nameof(EmailSettings),
        nameof(RestaurantHighlight),
        nameof(OperatorPrincipal),
        nameof(Restaurant),
        nameof(SocialLink),
        nameof(Section),
        nameof(Table),
        nameof(AdminPushSubscription),
        nameof(OperatorAgentCredential),
        nameof(OperatorRestaurantScope),
        nameof(RestaurantOccasionCatalogItem),
        nameof(Booking),
        nameof(OperatorAgentCredentialScope),
        nameof(AdminCredentialManagementAudit),
        nameof(AdminNotification),
        nameof(OperatorActionAudit),
        nameof(BookingOccasionSnapshot),
        nameof(WhatsAppHandoffAudit),
    ];

    public static IReadOnlyDictionary<string, int> SeededIdentityMaxima { get; } = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [nameof(AdminCredential)] = AdminCredentialId,
        [nameof(BrandSettings)] = BrandSettingsId,
        [nameof(ChannelMutationIdempotencyRecord)] = ChannelMutationIdempotencyRecordId,
        [nameof(EmailFailure)] = EmailFailureId,
        [nameof(EmailSettings)] = EmailSettingsId,
        [nameof(RestaurantHighlight)] = HighlightId,
        [nameof(OperatorPrincipal)] = OperatorPrincipalId,
        [nameof(Restaurant)] = RestaurantId,
        [nameof(SocialLink)] = SocialLinkId,
        [nameof(Section)] = SectionId,
        [nameof(Table)] = TableId,
        [nameof(AdminPushSubscription)] = AdminPushSubscriptionId,
        [nameof(OperatorAgentCredential)] = OperatorAgentCredentialId,
        [nameof(OperatorRestaurantScope)] = OperatorRestaurantScopeId,
        [nameof(RestaurantOccasionCatalogItem)] = RestaurantOccasionCatalogItemId,
        [nameof(Booking)] = BookingId,
        [nameof(OperatorAgentCredentialScope)] = OperatorAgentCredentialScopeId,
        [nameof(AdminCredentialManagementAudit)] = AdminCredentialManagementAuditId,
        [nameof(AdminNotification)] = AdminNotificationId,
        [nameof(OperatorActionAudit)] = OperatorActionAuditId,
        [nameof(BookingOccasionSnapshot)] = BookingOccasionSnapshotId,
        [nameof(WhatsAppHandoffAudit)] = WhatsAppHandoffAuditId,
    };

    public static async Task<string> CreateSqliteSourceAsync(string name, bool seedRichData = false)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{name}-{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={path}";

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        if (seedRichData)
        {
            SeedRichData(db);
        }

        await db.SaveChangesAsync();
        return path;
    }

    private static void SeedRichData(AppDbContext db)
    {
        (string passwordHash, string passwordSalt) = new PasswordService().Hash("TestPass123!");
        DateTime nowUtc = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

        db.AdminCredentials.Add(new AdminCredential
        {
            Id = AdminCredentialId,
            Email = "editor@test.com",
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = AdminRole.BookingEditor,
            IsActive = true,
            PvqQuestion = "favorite dish",
            PvqAnswerHash = "pvq-answer-hash",
            PvqAnswerSalt = "pvq-answer-salt",
        });

        db.BrandSettings.Add(new BrandSettings
        {
            Id = BrandSettingsId,
            AppName = "Migration Bistro",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            HeaderImageUrl = "https://cdn.example.com/hero.png",
            FaviconIcon = "fork-knife",
            WebsiteUrl = "https://migration-bistro.example.com",
            CopyrightText = "Migration Bistro",
            Subtitle = "Converted safely",
            HighlightsHeading = "Why migrate",
            HighlightsSubheading = "Everything preserved",
            HeaderImageFit = "Contain",
        });

        db.ChannelMutationIdempotencyRecords.Add(new ChannelMutationIdempotencyRecord
        {
            Id = ChannelMutationIdempotencyRecordId,
            Channel = "whatsapp",
            MutationScope = "handoff",
            IdempotencyKey = "handoff-req-1",
            Fingerprint = "fp-123",
            State = "Completed",
            ResultJson = "{\"status\":\"ok\"}",
            ReplayKey = "replay-123",
            CreatedAtUtc = nowUtc.AddMinutes(-45),
            CompletedAtUtc = nowUtc.AddMinutes(-44),
            ExpiresAtUtc = nowUtc.AddDays(7),
        });

        db.EmailFailures.Add(new EmailFailure
        {
            Id = EmailFailureId,
            BookingRef = "migrated-booking",
            RecipientEmail = "guest@example.com",
            ErrorMessage = "SMTP timeout",
            AttemptedAt = nowUtc.AddMinutes(-30),
        });

        db.EmailSettings.Add(new EmailSettings
        {
            Id = EmailSettingsId,
            Host = "smtp.example.com",
            Port = 587,
            Username = "smtp-user",
            EncryptedPassword = "ciphertext",
            EnableSsl = true,
            FromName = "Migration Bistro",
            FromEmail = "bookings@example.com",
            SendBookingConfirmations = true,
        });

        db.Highlights.Add(new RestaurantHighlight
        {
            Id = HighlightId,
            Title = "Private room",
            Body = "Seats up to 20 guests.",
            IconKey = "sparkles",
            SortOrder = 1,
            Link = "https://migration-bistro.example.com/private-room",
        });

        db.OperatorPrincipals.Add(new OperatorPrincipal
        {
            Id = OperatorPrincipalId,
            Identifier = "operator.maria",
            NormalizedIdentifier = "operator.maria",
            IsActive = true,
            CreatedAt = nowUtc.AddDays(-5),
            UpdatedAt = nowUtc.AddDays(-1),
        });

        db.Restaurants.Add(new Restaurant
        {
            Id = RestaurantId,
            Name = "Migration Bistro",
            Address = "123 Conversion Ave",
            OpenTime = "11:00",
            CloseTime = "23:00",
            OpenDays = "1,2,3,4,5,6,7",
            OpenHoursJson = "{\"5\":{\"open\":\"11:00\",\"close\":\"23:30\"}}",
            Timezone = "UTC",
            BookingsPausedUntil = nowUtc.AddHours(2),
            Tags = "tasting,chef-table",
            ImageUrl = "https://cdn.example.com/restaurant.png",
            Description = "Testing every migration edge.",
            MenuUrl = "https://migration-bistro.example.com/menu",
            IsArchived = false,
            IsWhatsAppTestEnabled = true,
            HandoffWhatsAppE164 = "+573009998877",
            WalkInOnly = false,
            WalkInDays = "1,2",
            DefaultBookingDurationMinutes = 90,
            BookingSlotIntervalMinutes = 15,
            MaxTableOversizeSeats = 2,
        });

        db.SocialLinks.Add(new SocialLink
        {
            Id = SocialLinkId,
            Label = "Instagram",
            Url = "https://instagram.com/migration-bistro",
            IconKey = "logo-instagram",
            SortOrder = 1,
        });

        db.Sections.Add(new Section
        {
            Id = SectionId,
            Name = "Main Room",
            SortOrder = 1,
            RestaurantId = RestaurantId,
        });

        db.Tables.Add(new Table
        {
            Id = TableId,
            Name = "T1",
            Seats = 4,
            SectionId = SectionId,
        });

        db.AdminPushSubscriptions.Add(new AdminPushSubscription
        {
            Id = AdminPushSubscriptionId,
            RestaurantId = RestaurantId,
            Endpoint = "https://push.example.com/subscriptions/1",
            P256dh = "p256dh-key",
            Auth = "auth-key",
            UserAgent = "MigrationSuite/1.0",
            CreatedAt = nowUtc.AddMinutes(-20),
        });

        db.OperatorAgentCredentials.Add(new OperatorAgentCredential
        {
            Id = OperatorAgentCredentialId,
            OperatorPrincipalId = OperatorPrincipalId,
            CredentialKeyId = "cred-key-1",
            TokenDigest = "digest-1",
            IssuedAt = nowUtc.AddDays(-4),
            ExpiresAt = nowUtc.AddDays(90),
            ExpirationPreset = "90d",
            RevokedAt = null,
            LastUsedAt = nowUtc.AddMinutes(-10),
            Notes = "Primary operator credential",
        });

        db.OperatorRestaurantScopes.Add(new OperatorRestaurantScope
        {
            Id = OperatorRestaurantScopeId,
            OperatorPrincipalId = OperatorPrincipalId,
            RestaurantId = RestaurantId,
            CreatedAt = nowUtc.AddDays(-4),
        });

        db.RestaurantOccasionCatalogItems.Add(new RestaurantOccasionCatalogItem
        {
            Id = RestaurantOccasionCatalogItemId,
            RestaurantId = RestaurantId,
            Name = "Birthday tasting",
            Description = "Includes dessert and candles.",
            EstimatedPriceCop = 250000,
            IsActive = true,
            SortOrder = 1,
            CreatedAtUtc = nowUtc.AddDays(-20),
            UpdatedAtUtc = nowUtc.AddDays(-1),
        });

        db.Bookings.Add(new Booking
        {
            Id = BookingId,
            RestaurantId = RestaurantId,
            SectionId = SectionId,
            TableId = TableId,
            Date = new DateTime(2026, 8, 1, 19, 30, 0, DateTimeKind.Utc),
            CustomerEmail = "guest@example.com",
            CustomerName = null,
            CustomerPhoneE164 = "+573001112233",
            CustomerPhoneNormalized = "573001112233",
            Seats = 2,
            SpecialRequests = "Window seat",
            BookingRef = "migrated-booking",
            EndTime = null,
            IsCancelled = false,
            CreatedByOperatorId = OperatorPrincipalId,
            CreatedViaChannel = "whatsapp",
            ConcurrencyToken = 4,
        });

        db.OperatorAgentCredentialScopes.Add(new OperatorAgentCredentialScope
        {
            Id = OperatorAgentCredentialScopeId,
            OperatorAgentCredentialId = OperatorAgentCredentialId,
            RestaurantId = RestaurantId,
            CreatedAt = nowUtc.AddDays(-4),
        });

        db.AdminCredentialManagementAudits.Add(new AdminCredentialManagementAudit
        {
            Id = AdminCredentialManagementAuditId,
            ActorAdminCredentialId = AdminCredentialId,
            ActorEmailSnapshot = "editor@test.com",
            TargetOperatorPrincipalId = OperatorPrincipalId,
            OperatorAgentCredentialId = OperatorAgentCredentialId,
            CredentialKeyIdSnapshot = "cred-key-1",
            TargetOperatorIdentifierSnapshot = "operator.maria",
            ScopeRestaurantIdsSnapshot = RestaurantId.ToString(),
            TtlHoursSnapshot = 2160,
            ExpirationPresetSnapshot = "90d",
            Action = "CredentialCreated",
            CreatedAtUtc = nowUtc.AddMinutes(-9),
        });

        db.AdminNotifications.Add(new AdminNotification
        {
            Id = AdminNotificationId,
            RestaurantId = RestaurantId,
            BookingId = BookingId,
            BookingRef = "migrated-booking",
            Type = NotificationType.OperatorEscalation,
            CustomerName = "Guest",
            BookingDate = new DateTime(2026, 8, 1, 19, 30, 0, DateTimeKind.Utc),
            Seats = 2,
            RestaurantName = "Migration Bistro",
            IsRead = false,
            CreatedAt = nowUtc.AddMinutes(-8),
            PushSentAt = nowUtc.AddMinutes(-7),
            PushError = null,
        });

        db.OperatorActionAudits.Add(new OperatorActionAudit
        {
            Id = OperatorActionAuditId,
            OperatorPrincipalId = OperatorPrincipalId,
            OperatorPrincipalIdSnapshot = OperatorPrincipalId,
            OperatorAgentCredentialId = OperatorAgentCredentialId,
            RestaurantId = RestaurantId,
            RestaurantIdSnapshot = RestaurantId,
            RestaurantNameSnapshot = "Migration Bistro",
            BookingId = BookingId,
            Action = "HandoffRequested",
            Outcome = "Escalated",
            Reason = "Customer requested human handoff",
            CorrelationId = "corr-123",
            CreatedAt = nowUtc.AddMinutes(-6),
        });

        db.BookingOccasionSnapshots.Add(new BookingOccasionSnapshot
        {
            Id = BookingOccasionSnapshotId,
            BookingId = BookingId,
            RestaurantOccasionCatalogItemId = RestaurantOccasionCatalogItemId,
            Name = "Birthday tasting",
            Description = "Includes dessert and candles.",
            EstimatedPriceCop = 250000,
            CreatedAtUtc = nowUtc.AddMinutes(-5),
        });

        db.WhatsAppHandoffAudits.Add(new WhatsAppHandoffAudit
        {
            Id = WhatsAppHandoffAuditId,
            RestaurantId = RestaurantId,
            BookingId = BookingId,
            VerifiedPhoneE164 = "+573001112233",
            VerifiedPhoneNormalized = "573001112233",
            SummarySnapshot = "Guest asked for a manual handoff after the birthday menu upsell.",
            HandoffDestinationSnapshot = "+573009998877",
            CreatedAtUtc = nowUtc.AddMinutes(-4),
        });
    }
}
