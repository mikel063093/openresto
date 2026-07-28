using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Infrastructure.Persistence.Repositories;

namespace OpenRestoApi.Tests.Services;

public sealed class ChannelIdempotencyServiceTests
{
    [Fact]
    public async Task RegisterMutationAsync_AllowsFirstAttempt_AndRejectsDuplicateKey()
    {
        using var db = TestDbFactory.Create(nameof(RegisterMutationAsync_AllowsFirstAttempt_AndRejectsDuplicateKey));
        var repository = new ChannelMutationIdempotencyRepository(db);
        var service = new ChannelIdempotencyService(repository);

        bool firstAccepted = await service.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:10");

        bool secondAccepted = await service.RegisterMutationAsync(
            channel: "whatsapp",
            mutationScope: "reservation.cancel",
            idempotencyKey: "wamid.123",
            fingerprint: "booking:10");

        Assert.True(firstAccepted);
        Assert.False(secondAccepted);
        Assert.Equal(1, await db.ChannelMutationIdempotencyRecords.CountAsync());
    }
}
