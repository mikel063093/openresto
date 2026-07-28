using CustomAccessibility.Attributes;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Infrastructure.Persistence.Repositories;

[OnlyAccessibleBy("OpenRestoApi.Extensions.ServiceCollectionExtensions")]
[OnlyAccessibleBy("OpenRestoApi.Tests.Services.ChannelIdempotencyServiceTests")]
[ExternalAccessAllowed]
internal sealed class ChannelMutationIdempotencyRepository(AppDbContext db) : IChannelMutationIdempotencyRepository
{
    private readonly AppDbContext _db = db;

    public Task<ChannelMutationIdempotencyRecord?> FindByIdempotencyKeyAsync(string channel, string mutationScope, string idempotencyKey)
        => _db.ChannelMutationIdempotencyRecords.FirstOrDefaultAsync(x =>
            x.Channel == channel &&
            x.MutationScope == mutationScope &&
            x.IdempotencyKey == idempotencyKey);

    public Task<ChannelMutationIdempotencyRecord?> FindByReplayKeyAsync(string channel, string replayKey)
        => _db.ChannelMutationIdempotencyRecords.FirstOrDefaultAsync(x =>
            x.Channel == channel &&
            x.ReplayKey == replayKey);

    public void Add(ChannelMutationIdempotencyRecord record)
        => _db.ChannelMutationIdempotencyRecords.Add(record);
}
