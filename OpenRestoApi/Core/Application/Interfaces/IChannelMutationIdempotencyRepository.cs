using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Interfaces;

public interface IChannelMutationIdempotencyRepository
{
    Task<ChannelMutationIdempotencyRecord?> FindAsync(string channel, string mutationScope, string idempotencyKey);
    Task AddAsync(ChannelMutationIdempotencyRecord record);
}
