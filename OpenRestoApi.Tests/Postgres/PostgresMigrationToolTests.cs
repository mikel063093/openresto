using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;
using OpenRestoApi.PostgresMigrationTool;

namespace OpenRestoApi.Tests.Postgres;

[Collection(PostgresIntegrationCollection.Name)]
public sealed class PostgresMigrationToolTests(PostgresTestHarness harness)
{
    private readonly PostgresTestHarness _harness = harness;

    [Fact]
    public async Task Converter_RefusesDirtyDestination()
    {
        if (!_harness.IsEnabled)
        {
            return;
        }

        await using PostgresTestDatabase destination = await _harness.CreateDatabaseAsync(nameof(Converter_RefusesDirtyDestination), seedApplicationData: false);
        await using (AppDbContext dirtyDb = CreatePostgresContext(destination.BootstrapConnectionString))
        {
            await dirtyDb.Database.MigrateAsync();
            dirtyDb.Restaurants.Add(new Restaurant { Name = "Already There" });
            await dirtyDb.SaveChangesAsync();
        }

        string sqlitePath = await PostgresMigrationTestData.CreateSqliteSourceAsync(nameof(Converter_RefusesDirtyDestination));
        string reportDir = Path.Combine(Path.GetTempPath(), "openresto-postgres-migration-reports", Guid.NewGuid().ToString("N"));

        var runner = new PostgresMigrationRunner();
        var command = new MigrationCommand(sqlitePath, destination.BootstrapConnectionString, reportDir, Directory.GetCurrentDirectory());

        MigrationRefusalException ex = await Assert.ThrowsAsync<MigrationRefusalException>(() => runner.RunAsync(command, CancellationToken.None));
        Assert.Contains("not clean", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Converter_ImportsEveryCurrentEntityAndFkChain_AndReseedsSequences_AndWritesRedactedReport()
    {
        if (!_harness.IsEnabled)
        {
            return;
        }

        await using PostgresTestDatabase destination = await _harness.CreateDatabaseAsync(nameof(Converter_ImportsEveryCurrentEntityAndFkChain_AndReseedsSequences_AndWritesRedactedReport), seedApplicationData: false);
        string sqlitePath = await PostgresMigrationTestData.CreateSqliteSourceAsync(nameof(Converter_ImportsEveryCurrentEntityAndFkChain_AndReseedsSequences_AndWritesRedactedReport), seedRichData: true);
        string reportDir = Path.Combine(Path.GetTempPath(), "openresto-postgres-migration-reports", Guid.NewGuid().ToString("N"));

        var runner = new PostgresMigrationRunner();
        var command = new MigrationCommand(sqlitePath, destination.BootstrapConnectionString, reportDir, Directory.GetCurrentDirectory());
        MigrationReport report = await runner.RunAsync(command, CancellationToken.None);

        Assert.True(File.Exists(report.ReportPath));
        string reportJson = await File.ReadAllTextAsync(report.ReportPath);
        Assert.DoesNotContain(destination.RuntimePassword, reportJson, StringComparison.Ordinal);
        JsonNode? reportNode = JsonNode.Parse(reportJson);
        Assert.True(reportNode?["destination"]?["passwordRedacted"]?.GetValue<bool>() ?? false);
        Assert.Equal(PostgresMigrationTestData.ImportedEntityNames.Count, report.Tables.Count);
        Assert.Equal(
            PostgresMigrationTestData.ImportedEntityNames.OrderBy(x => x, StringComparer.Ordinal),
            report.Tables.Select(x => x.Table).OrderBy(x => x, StringComparer.Ordinal));
        Assert.All(report.Tables, table =>
        {
            Assert.Equal(1, table.SourceRowCount);
            Assert.Equal(1, table.DestinationRowCount);
            Assert.Equal(PostgresMigrationTestData.SeededIdentityMaxima[table.Table], table.MaxIdentityValue);
        });

        await using AppDbContext db = CreatePostgresContext(destination.BootstrapConnectionString);
        Booking booking = await db.Bookings.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.BookingId, booking.Id);
        Assert.Equal(PostgresMigrationTestData.RestaurantId, booking.RestaurantId);
        Assert.Equal(PostgresMigrationTestData.SectionId, booking.SectionId);
        Assert.Equal(PostgresMigrationTestData.TableId, booking.TableId);
        Assert.Equal(DateTimeKind.Utc, booking.Date.Kind);
        Assert.Null(booking.CustomerName);
        Assert.Null(booking.EndTime);
        Assert.Equal(PostgresMigrationTestData.OperatorPrincipalId, booking.CreatedByOperatorId);
        Assert.Equal("whatsapp", booking.CreatedViaChannel);

        AdminCredential credential = await db.AdminCredentials.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.AdminCredentialId, credential.Id);
        Assert.Equal(AdminRole.BookingEditor, credential.Role);

        Restaurant restaurant = await db.Restaurants.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.RestaurantId, restaurant.Id);
        Assert.Equal("Migration Bistro", restaurant.Name);
        Assert.Equal("+573009998877", restaurant.HandoffWhatsAppE164);
        Assert.True(restaurant.IsWhatsAppTestEnabled);

        OperatorPrincipal principal = await db.OperatorPrincipals.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.OperatorPrincipalId, principal.Id);
        Assert.Equal("operator.maria", principal.NormalizedIdentifier);

        OperatorAgentCredential operatorCredential = await db.OperatorAgentCredentials.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.OperatorAgentCredentialId, operatorCredential.Id);
        Assert.Equal(PostgresMigrationTestData.OperatorPrincipalId, operatorCredential.OperatorPrincipalId);
        Assert.Equal("90d", operatorCredential.ExpirationPreset);

        OperatorRestaurantScope restaurantScope = await db.OperatorRestaurantScopes.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.OperatorRestaurantScopeId, restaurantScope.Id);
        Assert.Equal(PostgresMigrationTestData.OperatorPrincipalId, restaurantScope.OperatorPrincipalId);
        Assert.Equal(PostgresMigrationTestData.RestaurantId, restaurantScope.RestaurantId);

        OperatorAgentCredentialScope credentialScope = await db.OperatorAgentCredentialScopes.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.OperatorAgentCredentialScopeId, credentialScope.Id);
        Assert.Equal(PostgresMigrationTestData.OperatorAgentCredentialId, credentialScope.OperatorAgentCredentialId);
        Assert.Equal(PostgresMigrationTestData.RestaurantId, credentialScope.RestaurantId);

        RestaurantOccasionCatalogItem occasion = await db.RestaurantOccasionCatalogItems.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.RestaurantOccasionCatalogItemId, occasion.Id);
        Assert.Equal(PostgresMigrationTestData.RestaurantId, occasion.RestaurantId);

        BookingOccasionSnapshot snapshot = await db.BookingOccasionSnapshots.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.BookingOccasionSnapshotId, snapshot.Id);
        Assert.Equal(PostgresMigrationTestData.BookingId, snapshot.BookingId);
        Assert.Equal(PostgresMigrationTestData.RestaurantOccasionCatalogItemId, snapshot.RestaurantOccasionCatalogItemId);
        Assert.Equal(DateTimeKind.Utc, snapshot.CreatedAtUtc.Kind);

        AdminCredentialManagementAudit managementAudit = await db.AdminCredentialManagementAudits.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.AdminCredentialManagementAuditId, managementAudit.Id);
        Assert.Equal(PostgresMigrationTestData.AdminCredentialId, managementAudit.ActorAdminCredentialId);
        Assert.Equal(PostgresMigrationTestData.OperatorPrincipalId, managementAudit.TargetOperatorPrincipalId);
        Assert.Equal(PostgresMigrationTestData.OperatorAgentCredentialId, managementAudit.OperatorAgentCredentialId);

        AdminNotification notification = await db.AdminNotifications.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.AdminNotificationId, notification.Id);
        Assert.Equal(PostgresMigrationTestData.RestaurantId, notification.RestaurantId);
        Assert.Equal(PostgresMigrationTestData.BookingId, notification.BookingId);
        Assert.Equal(NotificationType.OperatorEscalation, notification.Type);

        OperatorActionAudit actionAudit = await db.OperatorActionAudits.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.OperatorActionAuditId, actionAudit.Id);
        Assert.Equal(PostgresMigrationTestData.OperatorPrincipalId, actionAudit.OperatorPrincipalId);
        Assert.Equal(PostgresMigrationTestData.OperatorAgentCredentialId, actionAudit.OperatorAgentCredentialId);
        Assert.Equal(PostgresMigrationTestData.BookingId, actionAudit.BookingId);
        Assert.Equal("Escalated", actionAudit.Outcome);

        WhatsAppHandoffAudit handoffAudit = await db.WhatsAppHandoffAudits.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.WhatsAppHandoffAuditId, handoffAudit.Id);
        Assert.Equal(PostgresMigrationTestData.RestaurantId, handoffAudit.RestaurantId);
        Assert.Equal(PostgresMigrationTestData.BookingId, handoffAudit.BookingId);
        Assert.Equal("+573009998877", handoffAudit.HandoffDestinationSnapshot);
        Assert.Equal(DateTimeKind.Utc, handoffAudit.CreatedAtUtc.Kind);

        ChannelMutationIdempotencyRecord idempotency = await db.ChannelMutationIdempotencyRecords.SingleAsync();
        Assert.Equal(PostgresMigrationTestData.ChannelMutationIdempotencyRecordId, idempotency.Id);
        Assert.Equal("Completed", idempotency.State);
        Assert.Equal(DateTimeKind.Utc, idempotency.CreatedAtUtc.Kind);

        Assert.Equal(PostgresMigrationTestData.EmailFailureId, (await db.EmailFailures.SingleAsync()).Id);
        Assert.Equal(PostgresMigrationTestData.EmailSettingsId, (await db.EmailSettings.SingleAsync()).Id);
        Assert.Equal(PostgresMigrationTestData.HighlightId, (await db.Highlights.SingleAsync()).Id);
        Assert.Equal(PostgresMigrationTestData.SocialLinkId, (await db.SocialLinks.SingleAsync()).Id);
        Assert.Equal(PostgresMigrationTestData.AdminPushSubscriptionId, (await db.AdminPushSubscriptions.SingleAsync()).Id);
        Assert.Equal(PostgresMigrationTestData.BrandSettingsId, (await db.BrandSettings.SingleAsync()).Id);

        await AssertSequencesWereReseededByPostImportWritesAsync(db);
    }

    [Fact]
    public async Task Converter_LateFailureRollsBackDestinationDataCompletely()
    {
        if (!_harness.IsEnabled)
        {
            return;
        }

        await using PostgresTestDatabase destination = await _harness.CreateDatabaseAsync(nameof(Converter_LateFailureRollsBackDestinationDataCompletely), seedApplicationData: false);
        string sqlitePath = await PostgresMigrationTestData.CreateSqliteSourceAsync(nameof(Converter_LateFailureRollsBackDestinationDataCompletely), seedRichData: true);
        string reportDir = Path.Combine(Path.GetTempPath(), "openresto-postgres-migration-reports", Guid.NewGuid().ToString("N"));
        int importedEntities = 0;

        var runner = new PostgresMigrationRunner
        {
            AfterEntityImportedAsync = (entityType, _) =>
            {
                importedEntities++;
                if (entityType == typeof(WhatsAppHandoffAudit))
                {
                    throw new InvalidOperationException("Intentional late failure after the final table import.");
                }

                return Task.CompletedTask;
            },
        };

        var command = new MigrationCommand(sqlitePath, destination.BootstrapConnectionString, reportDir, Directory.GetCurrentDirectory());

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(command, CancellationToken.None));
        Assert.Contains("Intentional late failure", ex.Message, StringComparison.Ordinal);
        Assert.Equal(PostgresMigrationTestData.ImportedEntityNames.Count, importedEntities);

        await using AppDbContext db = CreatePostgresContext(destination.BootstrapConnectionString);
        Assert.Empty(await db.AdminCredentials.ToListAsync());
        Assert.Empty(await db.BrandSettings.ToListAsync());
        Assert.Empty(await db.ChannelMutationIdempotencyRecords.ToListAsync());
        Assert.Empty(await db.EmailFailures.ToListAsync());
        Assert.Empty(await db.EmailSettings.ToListAsync());
        Assert.Empty(await db.Highlights.ToListAsync());
        Assert.Empty(await db.OperatorPrincipals.ToListAsync());
        Assert.Empty(await db.Restaurants.ToListAsync());
        Assert.Empty(await db.SocialLinks.ToListAsync());
        Assert.Empty(await db.Sections.ToListAsync());
        Assert.Empty(await db.Tables.ToListAsync());
        Assert.Empty(await db.AdminPushSubscriptions.ToListAsync());
        Assert.Empty(await db.OperatorAgentCredentials.ToListAsync());
        Assert.Empty(await db.OperatorRestaurantScopes.ToListAsync());
        Assert.Empty(await db.RestaurantOccasionCatalogItems.ToListAsync());
        Assert.Empty(await db.Bookings.ToListAsync());
        Assert.Empty(await db.OperatorAgentCredentialScopes.ToListAsync());
        Assert.Empty(await db.AdminCredentialManagementAudits.ToListAsync());
        Assert.Empty(await db.AdminNotifications.ToListAsync());
        Assert.Empty(await db.OperatorActionAudits.ToListAsync());
        Assert.Empty(await db.BookingOccasionSnapshots.ToListAsync());
        Assert.Empty(await db.WhatsAppHandoffAudits.ToListAsync());

        MigrationReport successfulRetry = await new PostgresMigrationRunner().RunAsync(command, CancellationToken.None);
        Assert.True(File.Exists(successfulRetry.ReportPath));
    }

    [Fact]
    public async Task Converter_RefusesReportDirectoryInsideRepository()
    {
        if (!_harness.IsEnabled)
        {
            return;
        }

        await using PostgresTestDatabase destination = await _harness.CreateDatabaseAsync(nameof(Converter_RefusesReportDirectoryInsideRepository), seedApplicationData: false);
        string sqlitePath = await PostgresMigrationTestData.CreateSqliteSourceAsync(nameof(Converter_RefusesReportDirectoryInsideRepository));
        string reportDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", Guid.NewGuid().ToString("N"));

        var runner = new PostgresMigrationRunner();
        var command = new MigrationCommand(sqlitePath, destination.BootstrapConnectionString, reportDir, Directory.GetCurrentDirectory());

        MigrationRefusalException ex = await Assert.ThrowsAsync<MigrationRefusalException>(() => runner.RunAsync(command, CancellationToken.None));
        Assert.Contains("outside the Git worktree", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertSequencesWereReseededByPostImportWritesAsync(AppDbContext db)
    {
        (string passwordHash, string passwordSalt) = new PasswordService().Hash("PostImportPass123!");
        DateTime baseUtc = new(2026, 8, 2, 18, 0, 0, DateTimeKind.Utc);

        var newAdmin = new AdminCredential
        {
            Email = "post-import-admin@test.com",
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = AdminRole.SuperAdmin,
            IsActive = true,
        };
        var newBrand = new BrandSettings
        {
            AppName = "Post Import Brand",
            PrimaryColor = "#778899",
        };
        var newChannelMutation = new ChannelMutationIdempotencyRecord
        {
            Channel = "whatsapp",
            MutationScope = "reservation",
            IdempotencyKey = "post-import-key",
            Fingerprint = "post-import-fp",
            State = "Completed",
            ReplayKey = "post-import-replay",
            CreatedAtUtc = baseUtc,
            CompletedAtUtc = baseUtc.AddMinutes(1),
            ExpiresAtUtc = baseUtc.AddDays(1),
        };
        var newEmailFailure = new EmailFailure
        {
            BookingRef = "post-import-booking",
            RecipientEmail = "post-import-guest@example.com",
            ErrorMessage = "temporary",
            AttemptedAt = baseUtc,
        };
        var newEmailSettings = new EmailSettings
        {
            Host = "smtp-after.example.com",
            Port = 2525,
            Username = "after",
            EncryptedPassword = "after-ciphertext",
            EnableSsl = false,
            FromName = "After Import",
            FromEmail = "after@example.com",
            SendBookingConfirmations = true,
        };
        var newHighlight = new RestaurantHighlight
        {
            Title = "After Import",
            Body = "Sequence reseed check",
            IconKey = "checkmark",
            SortOrder = 2,
        };
        var newSocialLink = new SocialLink
        {
            Label = "TikTok",
            Url = "https://tiktok.com/@post-import",
            IconKey = "logo-tiktok",
            SortOrder = 2,
        };
        var newOperator = new OperatorPrincipal
        {
            Identifier = "operator.after",
            NormalizedIdentifier = "operator.after",
            IsActive = true,
            CreatedAt = baseUtc,
        };
        var newRestaurant = new Restaurant
        {
            Name = "Post Import Bistro",
            Timezone = "UTC",
            HandoffWhatsAppE164 = "+573000000001",
        };
        var newSection = new Section
        {
            Name = "Post Import Room",
            SortOrder = 2,
            Restaurant = newRestaurant,
        };
        var newTable = new Table
        {
            Name = "T2",
            Seats = 2,
            Section = newSection,
        };
        var newPushSubscription = new AdminPushSubscription
        {
            Restaurant = newRestaurant,
            Endpoint = "https://push.example.com/subscriptions/2",
            P256dh = "p256dh-after",
            Auth = "auth-after",
            CreatedAt = baseUtc,
        };
        var newCredential = new OperatorAgentCredential
        {
            OperatorPrincipal = newOperator,
            CredentialKeyId = "cred-key-after",
            TokenDigest = "digest-after",
            IssuedAt = baseUtc,
            ExpiresAt = baseUtc.AddDays(30),
            ExpirationPreset = "30d",
        };
        var newRestaurantScope = new OperatorRestaurantScope
        {
            OperatorPrincipal = newOperator,
            Restaurant = newRestaurant,
            CreatedAt = baseUtc,
        };
        var newOccasion = new RestaurantOccasionCatalogItem
        {
            Restaurant = newRestaurant,
            Name = "Anniversary",
            EstimatedPriceCop = 150000,
            IsActive = true,
            SortOrder = 2,
            CreatedAtUtc = baseUtc,
            UpdatedAtUtc = baseUtc,
        };
        var newBooking = new Booking
        {
            Restaurant = newRestaurant,
            Section = newSection,
            Table = newTable,
            CreatedByOperator = newOperator,
            Date = baseUtc.AddDays(10),
            CustomerEmail = "post-import-guest@example.com",
            Seats = 2,
            BookingRef = "post-import-booking",
            CreatedViaChannel = "admin",
        };
        var newCredentialScope = new OperatorAgentCredentialScope
        {
            OperatorAgentCredential = newCredential,
            Restaurant = newRestaurant,
            CreatedAt = baseUtc,
        };

        await db.AddRangeAsync(
        [
            newAdmin,
            newBrand,
            newChannelMutation,
            newEmailFailure,
            newEmailSettings,
            newHighlight,
            newSocialLink,
            newOperator,
            newRestaurant,
            newSection,
            newTable,
            newPushSubscription,
            newCredential,
            newRestaurantScope,
            newOccasion,
            newBooking,
            newCredentialScope,
        ]);
        await db.SaveChangesAsync();

        var newManagementAudit = new AdminCredentialManagementAudit
        {
            ActorAdminCredential = newAdmin,
            ActorEmailSnapshot = newAdmin.Email,
            TargetOperatorPrincipal = newOperator,
            OperatorAgentCredential = newCredential,
            CredentialKeyIdSnapshot = newCredential.CredentialKeyId,
            TargetOperatorIdentifierSnapshot = newOperator.Identifier,
            ScopeRestaurantIdsSnapshot = newRestaurant.Id.ToString(),
            TtlHoursSnapshot = 720,
            ExpirationPresetSnapshot = "30d",
            Action = "CredentialCreated",
            CreatedAtUtc = baseUtc,
        };
        var newNotification = new AdminNotification
        {
            Restaurant = newRestaurant,
            Booking = newBooking,
            BookingRef = newBooking.BookingRef,
            Type = NotificationType.BookingCreated,
            CustomerName = "After Import Guest",
            BookingDate = newBooking.Date,
            Seats = newBooking.Seats,
            RestaurantName = newRestaurant.Name,
            CreatedAt = baseUtc,
        };
        var newActionAudit = new OperatorActionAudit
        {
            OperatorPrincipal = newOperator,
            OperatorPrincipalIdSnapshot = newOperator.Id,
            OperatorAgentCredential = newCredential,
            Restaurant = newRestaurant,
            RestaurantIdSnapshot = newRestaurant.Id,
            RestaurantNameSnapshot = newRestaurant.Name,
            Booking = newBooking,
            Action = "BookingCreated",
            Outcome = "Succeeded",
            CreatedAt = baseUtc,
        };
        var newOccasionSnapshot = new BookingOccasionSnapshot
        {
            BookingId = newBooking.Id,
            RestaurantOccasionCatalogItemId = newOccasion.Id,
            Name = newOccasion.Name,
            EstimatedPriceCop = newOccasion.EstimatedPriceCop,
            CreatedAtUtc = baseUtc,
        };
        var newHandoffAudit = new WhatsAppHandoffAudit
        {
            Restaurant = newRestaurant,
            Booking = newBooking,
            VerifiedPhoneE164 = "+573000000001",
            VerifiedPhoneNormalized = "573000000001",
            SummarySnapshot = "Sequence reseed verification handoff.",
            HandoffDestinationSnapshot = "+573000000001",
            CreatedAtUtc = baseUtc,
        };

        await db.AddRangeAsync(
        [
            newManagementAudit,
            newNotification,
            newActionAudit,
            newOccasionSnapshot,
            newHandoffAudit,
        ]);
        await db.SaveChangesAsync();

        Assert.True(newAdmin.Id > PostgresMigrationTestData.AdminCredentialId);
        Assert.True(newBrand.Id > PostgresMigrationTestData.BrandSettingsId);
        Assert.True(newChannelMutation.Id > PostgresMigrationTestData.ChannelMutationIdempotencyRecordId);
        Assert.True(newEmailFailure.Id > PostgresMigrationTestData.EmailFailureId);
        Assert.True(newEmailSettings.Id > PostgresMigrationTestData.EmailSettingsId);
        Assert.True(newHighlight.Id > PostgresMigrationTestData.HighlightId);
        Assert.True(newSocialLink.Id > PostgresMigrationTestData.SocialLinkId);
        Assert.True(newOperator.Id > PostgresMigrationTestData.OperatorPrincipalId);
        Assert.True(newRestaurant.Id > PostgresMigrationTestData.RestaurantId);
        Assert.True(newSection.Id > PostgresMigrationTestData.SectionId);
        Assert.True(newTable.Id > PostgresMigrationTestData.TableId);
        Assert.True(newPushSubscription.Id > PostgresMigrationTestData.AdminPushSubscriptionId);
        Assert.True(newCredential.Id > PostgresMigrationTestData.OperatorAgentCredentialId);
        Assert.True(newRestaurantScope.Id > PostgresMigrationTestData.OperatorRestaurantScopeId);
        Assert.True(newOccasion.Id > PostgresMigrationTestData.RestaurantOccasionCatalogItemId);
        Assert.True(newBooking.Id > PostgresMigrationTestData.BookingId);
        Assert.True(newCredentialScope.Id > PostgresMigrationTestData.OperatorAgentCredentialScopeId);
        Assert.True(newManagementAudit.Id > PostgresMigrationTestData.AdminCredentialManagementAuditId);
        Assert.True(newNotification.Id > PostgresMigrationTestData.AdminNotificationId);
        Assert.True(newActionAudit.Id > PostgresMigrationTestData.OperatorActionAuditId);
        Assert.True(newOccasionSnapshot.Id > PostgresMigrationTestData.BookingOccasionSnapshotId);
        Assert.True(newHandoffAudit.Id > PostgresMigrationTestData.WhatsAppHandoffAuditId);
    }

    private static AppDbContext CreatePostgresContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly("OpenRestoApi.PostgresMigrations"))
            .Options;
        return new AppDbContext(options);
    }
}
