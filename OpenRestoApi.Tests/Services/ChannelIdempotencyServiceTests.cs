using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;
using OpenRestoApi.Infrastructure.Persistence.Repositories;

namespace OpenRestoApi.Tests.Services;

public sealed class ChannelIdempotencyServiceTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"channel-idempotency-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RegisterMutationAsync_AllowsFirstAttempt_AndReplaysMatchingDuplicateKey()
    {
        await using AppDbContext db = CreateSqliteContext();
        var repository = new ChannelMutationIdempotencyRepository(db);
        var service = new ChannelIdempotencyService(repository);

        ChannelMutationRegistrationResult first = await service.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:10");

        ChannelMutationRegistrationResult second = await service.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:10");

        Assert.Equal(ChannelMutationRegistrationOutcome.Registered, first.Outcome);
        Assert.Equal(ChannelMutationRegistrationOutcome.Replayed, second.Outcome);
        Assert.Equal(first.Record.Id, second.Record.Id);
        Assert.Equal(1, await db.ChannelMutationIdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task RegisterMutationAsync_RejectsFingerprintReuseMismatch()
    {
        await using AppDbContext db = CreateSqliteContext();
        var repository = new ChannelMutationIdempotencyRepository(db);
        var service = new ChannelIdempotencyService(repository);

        await service.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:10");

        ConflictException ex = await Assert.ThrowsAsync<ConflictException>(() => service.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:11"));

        Assert.Contains("idempotency", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterMutationAsync_HandlesConcurrentInsertRaceAcrossSeparateDbContexts()
    {
        await using AppDbContext setupDb = CreateSqliteContext();
        await setupDb.Database.EnsureCreatedAsync();

        await using AppDbContext firstDb = CreateSqliteContext();
        await using AppDbContext secondDb = CreateSqliteContext();
        var gate = new Barrier(2);

        var firstService = new ChannelIdempotencyService(
            new RacingRepository(new ChannelMutationIdempotencyRepository(firstDb), gate));
        var secondService = new ChannelIdempotencyService(
            new RacingRepository(new ChannelMutationIdempotencyRepository(secondDb), gate));

        Task<ChannelMutationRegistrationResult> firstTask = firstService.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "wamid.race",
            fingerprint: "booking:20");
        Task<ChannelMutationRegistrationResult> secondTask = secondService.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "wamid.race",
            fingerprint: "booking:20");

        ChannelMutationRegistrationResult[] results = await Task.WhenAll(firstTask, secondTask);

        Assert.Contains(results, x => x.Outcome == ChannelMutationRegistrationOutcome.Registered);
        Assert.Contains(results, x => x.Outcome == ChannelMutationRegistrationOutcome.Replayed);

        await using AppDbContext assertDb = CreateSqliteContext();
        Assert.Equal(1, await assertDb.ChannelMutationIdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task RegisterMutationAsync_ReportsConflictAfterConcurrentInsertRace_WhenFingerprintDiffers()
    {
        await using AppDbContext setupDb = CreateSqliteContext();
        await setupDb.Database.EnsureCreatedAsync();

        await using AppDbContext firstDb = CreateSqliteContext();
        await using AppDbContext secondDb = CreateSqliteContext();
        var gate = new Barrier(2);

        var firstService = new ChannelIdempotencyService(
            new RacingRepository(new ChannelMutationIdempotencyRepository(firstDb), gate));
        var secondService = new ChannelIdempotencyService(
            new RacingRepository(new ChannelMutationIdempotencyRepository(secondDb), gate));

        Task<ChannelMutationRegistrationResult> firstTask = firstService.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "wamid.race-conflict",
            fingerprint: "booking:20");
        Task secondTask = secondService.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "wamid.race-conflict",
            fingerprint: "booking:21");

        ChannelMutationRegistrationResult firstResult = await firstTask;
        ConflictException ex = await Assert.ThrowsAsync<ConflictException>(async () => await secondTask);

        Assert.Equal(ChannelMutationRegistrationOutcome.Registered, firstResult.Outcome);
        Assert.Contains("idempotency", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private AppDbContext CreateSqliteContext()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath};Cache=Shared");
        connection.Open();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private sealed class RacingRepository(IChannelMutationIdempotencyRepository inner, Barrier gate)
        : IChannelMutationIdempotencyRepository
    {
        private readonly IChannelMutationIdempotencyRepository _inner = inner;
        private readonly Barrier _gate = gate;

        public async Task<ChannelMutationIdempotencyRecord?> FindAsync(string channel, string mutationScope, string idempotencyKey)
        {
            ChannelMutationIdempotencyRecord? record = await _inner.FindAsync(channel, mutationScope, idempotencyKey);
            _gate.SignalAndWait(TimeSpan.FromSeconds(10));
            return record;
        }

        public Task AddAsync(ChannelMutationIdempotencyRecord record)
            => _inner.AddAsync(record);
    }
}
