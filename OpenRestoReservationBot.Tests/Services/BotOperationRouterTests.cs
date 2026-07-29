using OpenRestoReservationBot.Contracts;
using OpenRestoReservationBot.Services;

namespace OpenRestoReservationBot.Tests.Services;

public sealed class BotOperationRouterTests
{
    private readonly FakeAvailabilityClient _availabilityClient = new();
    private readonly FakePrivateChannelClient _privateClient = new();
    private readonly FakeContextAccessor _contextAccessor = new();

    [Fact]
    public async Task Availability_RoutesToAvailabilityClient()
    {
        BotOperationRouter router = CreateRouter();

        BotOperationResponse response = await router.ExecuteAsync(
            new BotOperationRequest
            {
                Operation = ReservationBotOperation.Availability,
                Availability = new AvailabilityLookupRequest
                {
                    RestaurantId = 1,
                    Date = new DateTime(2026, 8, 1, 19, 0, 0, DateTimeKind.Utc),
                    Seats = 2
                }
            },
            CancellationToken.None);

        Assert.NotNull(response.Availability);
        Assert.Equal(1, _availabilityClient.LookupCalls);
    }

    [Fact]
    public async Task Create_RoutesToPrivateClient()
    {
        BotOperationRouter router = CreateRouter();

        BotOperationResponse response = await router.ExecuteAsync(
            new BotOperationRequest
            {
                Operation = ReservationBotOperation.Create,
                Create = new ReservationCreateRequest
                {
                    RestaurantId = 2,
                    Date = new DateTime(2026, 8, 2, 20, 0, 0, DateTimeKind.Utc),
                    Seats = 3,
                    CustomerEmail = "lina@example.com",
                    CustomerName = "Lina",
                    Confirmed = true,
                    IdempotencyKey = "create-1"
                }
            },
            CancellationToken.None);

        Assert.NotNull(response.Reservation);
        Assert.Equal(1, _privateClient.CreateCalls);
    }

    [Fact]
    public async Task List_RoutesToPrivateClient()
    {
        BotOperationRouter router = CreateRouter();

        BotOperationResponse response = await router.ExecuteAsync(
            new BotOperationRequest
            {
                Operation = ReservationBotOperation.List,
                List = new ReservationListRequest()
            },
            CancellationToken.None);

        Assert.NotNull(response.Reservations);
        Assert.Equal(1, _privateClient.ListCalls);
    }

    [Fact]
    public async Task Detail_RoutesToPrivateClient()
    {
        BotOperationRouter router = CreateRouter();

        BotOperationResponse response = await router.ExecuteAsync(
            new BotOperationRequest
            {
                Operation = ReservationBotOperation.Detail,
                Detail = new ReservationDetailRequest { ReservationId = 44 }
            },
            CancellationToken.None);

        Assert.NotNull(response.Reservation);
        Assert.Equal(1, _privateClient.DetailCalls);
    }

    [Fact]
    public async Task Update_RoutesToPrivateClient()
    {
        BotOperationRouter router = CreateRouter();

        BotOperationResponse response = await router.ExecuteAsync(
            new BotOperationRequest
            {
                Operation = ReservationBotOperation.Update,
                Update = new ReservationUpdateRequest
                {
                    ReservationId = 44,
                    Date = new DateTime(2026, 8, 4, 20, 0, 0, DateTimeKind.Utc),
                    Seats = 4,
                    Confirmed = true,
                    IdempotencyKey = "update-1",
                    ExpectedConcurrencyToken = 5
                }
            },
            CancellationToken.None);

        Assert.NotNull(response.Reservation);
        Assert.Equal(1, _privateClient.UpdateCalls);
    }

    [Fact]
    public async Task Cancel_RoutesToPrivateClient()
    {
        BotOperationRouter router = CreateRouter();

        BotOperationResponse response = await router.ExecuteAsync(
            new BotOperationRequest
            {
                Operation = ReservationBotOperation.Cancel,
                Cancel = new ReservationCancelRequest
                {
                    ReservationId = 44,
                    Confirmed = true,
                    IdempotencyKey = "cancel-1",
                    ExpectedConcurrencyToken = 2
                }
            },
            CancellationToken.None);

        Assert.NotNull(response.Cancellation);
        Assert.Equal(1, _privateClient.CancelCalls);
    }

    [Fact]
    public async Task OccasionCatalog_RoutesToPrivateClient()
    {
        BotOperationRouter router = CreateRouter();

        BotOperationResponse response = await router.ExecuteAsync(
            new BotOperationRequest
            {
                Operation = ReservationBotOperation.OccasionCatalog,
                OccasionCatalog = new OccasionCatalogLookupRequest { RestaurantId = 5 }
            },
            CancellationToken.None);

        Assert.NotNull(response.OccasionCatalog);
        Assert.Equal(1, _privateClient.OccasionCatalogCalls);
    }

    [Fact]
    public async Task Handoff_RoutesToPrivateClient()
    {
        BotOperationRouter router = CreateRouter();

        BotOperationResponse response = await router.ExecuteAsync(
            new BotOperationRequest
            {
                Operation = ReservationBotOperation.Handoff,
                Handoff = new ReservationHandoffRequest
                {
                    RestaurantId = 6,
                    BookingId = 88,
                    Summary = "Cliente necesita apoyo.",
                    Confirmed = true,
                    IdempotencyKey = "handoff-1"
                }
            },
            CancellationToken.None);

        Assert.NotNull(response.Handoff);
        Assert.Equal(1, _privateClient.HandoffCalls);
    }

    private BotOperationRouter CreateRouter() => new(_availabilityClient, _privateClient, _contextAccessor);

    private sealed class FakeContextAccessor : IBotRequestContextAccessor
    {
        public BotRequestContext GetRequiredContext() => new("assertion.raw.token", "corr-router-tests");
    }

    private sealed class FakeAvailabilityClient : IOpenRestoAvailabilityClient
    {
        public int LookupCalls { get; private set; }

        public Task<OpenRestoAvailabilityDto> LookupAsync(AvailabilityLookupRequest request, string correlationId, CancellationToken cancellationToken)
        {
            LookupCalls++;
            return Task.FromResult(new OpenRestoAvailabilityDto { RestaurantId = request.RestaurantId, Date = request.Date, Slots = [] });
        }
    }

    private sealed class FakePrivateChannelClient : IOpenRestoPrivateChannelClient
    {
        public int ListCalls { get; private set; }
        public int DetailCalls { get; private set; }
        public int CreateCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int CancelCalls { get; private set; }
        public int OccasionCatalogCalls { get; private set; }
        public int HandoffCalls { get; private set; }

        public Task<IReadOnlyList<OpenRestoReservationDto>> ListReservationsAsync(BotRequestContext context, CancellationToken cancellationToken)
        {
            ListCalls++;
            return Task.FromResult<IReadOnlyList<OpenRestoReservationDto>>([]);
        }

        public Task<OpenRestoReservationDto> GetReservationAsync(int reservationId, BotRequestContext context, CancellationToken cancellationToken)
        {
            DetailCalls++;
            return Task.FromResult(new OpenRestoReservationDto { Id = reservationId });
        }

        public Task<OpenRestoReservationDto> CreateReservationAsync(ReservationCreateRequest request, BotRequestContext context, CancellationToken cancellationToken)
        {
            CreateCalls++;
            return Task.FromResult(new OpenRestoReservationDto { Id = 1, RestaurantId = request.RestaurantId });
        }

        public Task<OpenRestoReservationDto> UpdateReservationAsync(ReservationUpdateRequest request, BotRequestContext context, CancellationToken cancellationToken)
        {
            UpdateCalls++;
            return Task.FromResult(new OpenRestoReservationDto { Id = request.ReservationId });
        }

        public Task<OpenRestoReservationActionResultDto> CancelReservationAsync(ReservationCancelRequest request, BotRequestContext context, CancellationToken cancellationToken)
        {
            CancelCalls++;
            return Task.FromResult(new OpenRestoReservationActionResultDto { Success = true, Message = "Cancelada." });
        }

        public Task<IReadOnlyList<OpenRestoOccasionCatalogItemDto>> GetOccasionCatalogAsync(int restaurantId, BotRequestContext context, CancellationToken cancellationToken)
        {
            OccasionCatalogCalls++;
            return Task.FromResult<IReadOnlyList<OpenRestoOccasionCatalogItemDto>>([]);
        }

        public Task<OpenRestoHandoffResultDto> CreateHandoffAsync(ReservationHandoffRequest request, BotRequestContext context, CancellationToken cancellationToken)
        {
            HandoffCalls++;
            return Task.FromResult(new OpenRestoHandoffResultDto { AuditId = 1, RestaurantId = request.RestaurantId });
        }
    }
}
