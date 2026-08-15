using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;
using OpenRestoApi.Infrastructure.Persistence.Repositories;
using OpenRestoApi.Tests.Holds;

namespace OpenRestoApi.Tests.Services;

public sealed class ChannelIdempotencyServiceTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"channel-idempotency-{Guid.NewGuid():N}.db");
    private readonly FakeClock _clock = new(new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc));

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExecuteAsync_AllowsFirstAttempt_AndReplaysMatchingDuplicateKey()
    {
        await using AppDbContext db = CreateSqliteContext();
        var repository = new ChannelMutationIdempotencyRepository(db);
        var service = new ChannelIdempotencyService(repository, db, _clock);

        ChannelMutationExecutionResult<string> first = await service.ExecuteAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:10",
            operation: () => Task.FromResult("ok"));

        ChannelMutationExecutionResult<string> second = await service.ExecuteAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:10",
            operation: () => Task.FromResult("unexpected"));

        Assert.Equal(ChannelMutationExecutionOutcome.Executed, first.Outcome);
        Assert.Equal(ChannelMutationExecutionOutcome.Replayed, second.Outcome);
        Assert.Equal("ok", second.Result);
        Assert.Equal(1, await db.ChannelMutationIdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_RejectsFingerprintReuseMismatch()
    {
        await using AppDbContext db = CreateSqliteContext();
        var repository = new ChannelMutationIdempotencyRepository(db);
        var service = new ChannelIdempotencyService(repository, db, _clock);

        await service.ExecuteAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:10",
            operation: () => Task.FromResult("ok"));

        ConflictException ex = await Assert.ThrowsAsync<ConflictException>(() => service.ExecuteAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:11",
            operation: () => Task.FromResult("bad")));

        Assert.Contains("idempotency", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_RolledBackBusinessFailure_DoesNotConsumeKey()
    {
        await using AppDbContext db = CreateSqliteContext();
        var repository = new ChannelMutationIdempotencyRepository(db);
        var service = new ChannelIdempotencyService(repository, db, _clock);

        await Assert.ThrowsAsync<ValidationException>(() => service.ExecuteAsync<string>(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "validation-before-idempotency",
            fingerprint: "booking:20",
            operation: () => throw new ValidationException("bad request")));

        ChannelMutationExecutionResult<string> retry = await service.ExecuteAsync(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "validation-before-idempotency",
            fingerprint: "booking:20",
            operation: () => Task.FromResult("recovered"));

        Assert.Equal(ChannelMutationExecutionOutcome.Executed, retry.Outcome);
        Assert.Equal("recovered", retry.Result);
        Assert.Equal(1, await db.ChannelMutationIdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_HandlesConcurrentInsertRaceAcrossSeparateDbContexts()
    {
        await using AppDbContext setupDb = CreateSqliteContext();
        await setupDb.Database.EnsureCreatedAsync();

        await using AppDbContext firstDb = CreateSqliteContext();
        await using AppDbContext secondDb = CreateSqliteContext();
        var gate = new Barrier(2);

        var firstService = new ChannelIdempotencyService(
            new RacingRepository(new ChannelMutationIdempotencyRepository(firstDb), gate),
            firstDb,
            _clock);
        var secondService = new ChannelIdempotencyService(
            new RacingRepository(new ChannelMutationIdempotencyRepository(secondDb), gate),
            secondDb,
            _clock);

        Task<ChannelMutationExecutionResult<string>> firstTask = firstService.ExecuteAsync(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "wamid.race",
            fingerprint: "booking:20",
            operation: () => Task.FromResult("winner"));
        Task<ChannelMutationExecutionResult<string>> secondTask = secondService.ExecuteAsync(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "wamid.race",
            fingerprint: "booking:20",
            operation: () => Task.FromResult("loser"));

        ChannelMutationExecutionResult<string>[] results = await Task.WhenAll(firstTask, secondTask);

        Assert.Contains(results, x => x.Outcome == ChannelMutationExecutionOutcome.Executed);
        Assert.Contains(results, x => x.Outcome == ChannelMutationExecutionOutcome.Replayed);

        await using AppDbContext assertDb = CreateSqliteContext();
        Assert.Equal(1, await assertDb.ChannelMutationIdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_ReportsConflictAfterConcurrentInsertRace_WhenFingerprintDiffers()
    {
        await using AppDbContext setupDb = CreateSqliteContext();
        await setupDb.Database.EnsureCreatedAsync();

        await using AppDbContext firstDb = CreateSqliteContext();
        await using AppDbContext secondDb = CreateSqliteContext();
        var gate = new Barrier(2);

        var firstService = new ChannelIdempotencyService(
            new RacingRepository(new ChannelMutationIdempotencyRepository(firstDb), gate),
            firstDb,
            _clock);
        var secondService = new ChannelIdempotencyService(
            new RacingRepository(new ChannelMutationIdempotencyRepository(secondDb), gate),
            secondDb,
            _clock);

        Task<ChannelMutationExecutionResult<string>> firstTask = firstService.ExecuteAsync(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "wamid.race-conflict",
            fingerprint: "booking:20",
            operation: () => Task.FromResult("winner"));
        Task secondTask = secondService.ExecuteAsync(
            channel: "whatsapp",
            mutationScope: "reservation.update",
            idempotencyKey: "wamid.race-conflict",
            fingerprint: "booking:21",
            operation: () => Task.FromResult("loser"));

        ChannelMutationExecutionResult<string> firstResult = await firstTask;
        ConflictException ex = await Assert.ThrowsAsync<ConflictException>(async () => await secondTask);

        Assert.Equal(ChannelMutationExecutionOutcome.Executed, firstResult.Outcome);
        Assert.Contains("idempotency", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterReplayKeyAsync_PurgesExpiredReplayRecordBeforeUniquenessCheck()
    {
        await using AppDbContext db = CreateSqliteContext();
        db.ChannelMutationIdempotencyRecords.Add(new ChannelMutationIdempotencyRecord
        {
            Channel = "whatsapp_assertion",
            MutationScope = "reservations.read",
            IdempotencyKey = "expired-jti",
            ReplayKey = "expired-jti",
            Fingerprint = "GET:/api/private/channels/whatsapp/reservations:reservations.read",
            State = ChannelMutationState.Consumed,
            CreatedAtUtc = _clock.UtcNow.AddMinutes(-10),
            CompletedAtUtc = _clock.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = _clock.UtcNow.AddMinutes(-1),
        });
        await db.SaveChangesAsync();

        var repository = new ChannelMutationIdempotencyRepository(db);
        var service = new ChannelIdempotencyService(repository, db, _clock);

        ChannelReplayRegistrationResult result = await service.RegisterReplayKeyAsync(
            channel: "whatsapp_assertion",
            replayKey: "expired-jti",
            mutationScope: "reservations.read",
            fingerprint: "GET:/api/private/channels/whatsapp/restaurants:reservations.read",
            expiresAtUtc: _clock.UtcNow.AddMinutes(5));

        Assert.False(result.WasReplayed);

        List<ChannelMutationIdempotencyRecord> records = await db.ChannelMutationIdempotencyRecords
            .Where(x => x.Channel == "whatsapp_assertion" && x.ReplayKey == "expired-jti")
            .ToListAsync();
        ChannelMutationIdempotencyRecord persisted = Assert.Single(records);
        Assert.True(persisted.ExpiresAtUtc > _clock.UtcNow);
        Assert.Equal("GET:/api/private/channels/whatsapp/restaurants:reservations.read", persisted.Fingerprint);
    }

    [Fact]
    public async Task RegisterReplayKeyAsync_DoesNotReuseStillValidLongLivedAssertion_BeforeActualExpiry_AndPurgesAfterExpiry()
    {
        await using AppDbContext db = CreateSqliteContext();
        var repository = new ChannelMutationIdempotencyRepository(db);
        var service = new ChannelIdempotencyService(repository, db, _clock);
        DateTime originalExpiry = _clock.UtcNow.AddMinutes(9);

        ChannelReplayRegistrationResult first = await service.RegisterReplayKeyAsync(
            channel: "whatsapp_assertion",
            replayKey: "long-lived-jti",
            mutationScope: "reservations.read",
            fingerprint: "GET:/api/private/channels/whatsapp/reservations:reservations.read",
            expiresAtUtc: originalExpiry);

        Assert.False(first.WasReplayed);

        _clock.Advance(TimeSpan.FromMinutes(6));

        ChannelReplayRegistrationResult beforeExpiryReplay = await service.RegisterReplayKeyAsync(
            channel: "whatsapp_assertion",
            replayKey: "long-lived-jti",
            mutationScope: "reservations.read",
            fingerprint: "GET:/api/private/channels/whatsapp/reservations:reservations.read",
            expiresAtUtc: _clock.UtcNow.AddMinutes(2));

        Assert.True(beforeExpiryReplay.WasReplayed);
        Assert.Equal(first.Record.Id, beforeExpiryReplay.Record.Id);
        Assert.Equal(originalExpiry, beforeExpiryReplay.Record.ExpiresAtUtc);

        _clock.Advance(TimeSpan.FromMinutes(4));

        ChannelReplayRegistrationResult afterExpiryReplay = await service.RegisterReplayKeyAsync(
            channel: "whatsapp_assertion",
            replayKey: "long-lived-jti",
            mutationScope: "reservations.read",
            fingerprint: "GET:/api/private/channels/whatsapp/reservations:reservations.read",
            expiresAtUtc: _clock.UtcNow.AddMinutes(2));

        Assert.False(afterExpiryReplay.WasReplayed);
        Assert.NotEqual(first.Record.Id, afterExpiryReplay.Record.Id);

        List<ChannelMutationIdempotencyRecord> records = await db.ChannelMutationIdempotencyRecords
            .Where(x => x.Channel == "whatsapp_assertion" && x.ReplayKey == "long-lived-jti")
            .OrderBy(x => x.Id)
            .ToListAsync();
        ChannelMutationIdempotencyRecord persisted = Assert.Single(records);
        Assert.Equal(afterExpiryReplay.Record.Id, persisted.Id);
        Assert.Equal(_clock.UtcNow.AddMinutes(2), persisted.ExpiresAtUtc);
    }

    private AppDbContext CreateSqliteContext()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath};Cache=Shared");
        connection.Open();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection, sqliteOptions =>
                sqliteOptions.ExecutionStrategy(dependencies => new SqliteRetryingExecutionStrategy(dependencies)))
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
        private int _findCount;

        public async Task<ChannelMutationIdempotencyRecord?> FindByIdempotencyKeyAsync(string channel, string mutationScope, string idempotencyKey)
        {
            ChannelMutationIdempotencyRecord? record = await _inner.FindByIdempotencyKeyAsync(channel, mutationScope, idempotencyKey);
            if (Interlocked.Increment(ref _findCount) == 1)
            {
                _gate.SignalAndWait(TimeSpan.FromSeconds(10));
            }
            return record;
        }

        public Task<ChannelMutationIdempotencyRecord?> FindByReplayKeyAsync(string channel, string replayKey)
            => _inner.FindByReplayKeyAsync(channel, replayKey);

        public void Add(ChannelMutationIdempotencyRecord record)
            => _inner.Add(record);
    }
}
