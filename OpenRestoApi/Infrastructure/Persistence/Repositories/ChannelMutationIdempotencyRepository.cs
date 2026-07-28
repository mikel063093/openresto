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

    public Task<ChannelMutationIdempotencyRecord?> FindAsync(string channel, string mutationScope, string idempotencyKey)
        => _db.ChannelMutationIdempotencyRecords.FirstOrDefaultAsync(x =>
            x.Channel == channel &&
            x.MutationScope == mutationScope &&
            x.IdempotencyKey == idempotencyKey);

    public async Task AddAsync(ChannelMutationIdempotencyRecord record)
    {
        _db.ChannelMutationIdempotencyRecords.Add(record);
        await _db.SaveChangesAsync();
    }
}
