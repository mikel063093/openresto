using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public sealed class ChannelIdempotencyService(
    IChannelMutationIdempotencyRepository repository,
    AppDbContext db)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IChannelMutationIdempotencyRepository _repository = repository;
    private readonly AppDbContext _db = db;

    public async Task<ChannelMutationExecutionResult<T>> ExecuteAsync<T>(
        string channel,
        string mutationScope,
        string idempotencyKey,
        string fingerprint,
        Func<Task<T>> operation)
    {
        return await _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync()
                : null;

            ChannelMutationIdempotencyRecord? existing = await _repository.FindByIdempotencyKeyAsync(channel, mutationScope, idempotencyKey);
            if (existing != null)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                return ReplayCompleted<T>(existing, fingerprint);
            }

            var record = new ChannelMutationIdempotencyRecord
            {
                Channel = channel,
                MutationScope = mutationScope,
                IdempotencyKey = idempotencyKey,
                Fingerprint = fingerprint,
                State = ChannelMutationState.Pending,
                CreatedAtUtc = DateTime.UtcNow,
            };

            _repository.Add(record);

            try
            {
                T result = await operation();
                record.State = ChannelMutationState.Completed;
                record.ResultJson = JsonSerializer.Serialize(result, SerializerOptions);
                record.CompletedAtUtc = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                if (transaction is not null)
                {
                    await transaction.CommitAsync();
                }

                return new ChannelMutationExecutionResult<T>(ChannelMutationExecutionOutcome.Executed, result, record);
            }
            catch (DbUpdateException)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                _db.ChangeTracker.Clear();

                existing = await _repository.FindByIdempotencyKeyAsync(channel, mutationScope, idempotencyKey);
                if (existing != null)
                {
                    return ReplayCompleted<T>(existing, fingerprint);
                }

                throw;
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                _db.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public async Task<ChannelReplayRegistrationResult> RegisterReplayKeyAsync(
        string channel,
        string replayKey,
        string mutationScope,
        string fingerprint,
        DateTime expiresAtUtc)
    {
        await PurgeExpiredReplayRecordsAsync(channel, replayKey);

        ChannelMutationIdempotencyRecord? existing = await _repository.FindByReplayKeyAsync(channel, replayKey);
        if (existing != null)
        {
            return new ChannelReplayRegistrationResult(true, existing);
        }

        var record = new ChannelMutationIdempotencyRecord
        {
            Channel = channel,
            MutationScope = mutationScope,
            IdempotencyKey = replayKey,
            ReplayKey = replayKey,
            Fingerprint = fingerprint,
            State = ChannelMutationState.Consumed,
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
        };

        _repository.Add(record);

        try
        {
            await _db.SaveChangesAsync();
            return new ChannelReplayRegistrationResult(false, record);
        }
        catch (DbUpdateException)
        {
            ChannelMutationIdempotencyRecord replayedRecord = await _repository.FindByReplayKeyAsync(channel, replayKey)
                ?? throw new InvalidOperationException("Replay record was created concurrently but could not be reloaded.");
            return new ChannelReplayRegistrationResult(
                true,
                replayedRecord);
        }
    }

    private async Task PurgeExpiredReplayRecordsAsync(string channel, string replayKey)
    {
        DateTime nowUtc = DateTime.UtcNow;
        List<ChannelMutationIdempotencyRecord> expiredRecords = await _db.ChannelMutationIdempotencyRecords
            .Where(x =>
                x.Channel == channel &&
                x.ReplayKey == replayKey &&
                x.ExpiresAtUtc.HasValue &&
                x.ExpiresAtUtc.Value <= nowUtc)
            .OrderBy(x => x.Id)
            .Take(8)
            .ToListAsync();

        if (expiredRecords.Count == 0)
        {
            return;
        }

        _db.ChannelMutationIdempotencyRecords.RemoveRange(expiredRecords);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private static ChannelMutationExecutionResult<T> ReplayCompleted<T>(
        ChannelMutationIdempotencyRecord existing,
        string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            throw new ConflictException("The idempotency key was already used for a different mutation fingerprint.");
        }

        if (!string.Equals(existing.State, ChannelMutationState.Completed, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(existing.ResultJson))
        {
            throw new ConflictException("The idempotency key is already in use.");
        }

        T result = JsonSerializer.Deserialize<T>(existing.ResultJson, SerializerOptions)
            ?? throw new InvalidOperationException("Stored idempotent result payload could not be deserialized.");

        return new ChannelMutationExecutionResult<T>(ChannelMutationExecutionOutcome.Replayed, result, existing);
    }
}

public static class ChannelMutationState
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Consumed = "consumed";
}

public enum ChannelMutationExecutionOutcome
{
    Executed = 1,
    Replayed = 2,
}

public sealed record ChannelMutationExecutionResult<T>(
    ChannelMutationExecutionOutcome Outcome,
    T Result,
    ChannelMutationIdempotencyRecord Record);

public sealed record ChannelReplayRegistrationResult(
    bool WasReplayed,
    ChannelMutationIdempotencyRecord Record);
