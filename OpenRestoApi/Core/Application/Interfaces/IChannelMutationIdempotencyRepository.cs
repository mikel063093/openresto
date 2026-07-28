using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Interfaces;

public interface IChannelMutationIdempotencyRepository
{
    Task<ChannelMutationIdempotencyRecord?> FindByIdempotencyKeyAsync(string channel, string mutationScope, string idempotencyKey);
    Task<ChannelMutationIdempotencyRecord?> FindByReplayKeyAsync(string channel, string replayKey);
    void Add(ChannelMutationIdempotencyRecord record);
}
