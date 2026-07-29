using OpenRestoReservationBot.Contracts;

namespace OpenRestoReservationBot.Services;

public interface IBotOperationRouter
{
    Task<BotOperationResponse> ExecuteAsync(BotOperationRequest request, CancellationToken cancellationToken);
}

public sealed class BotOperationRouter(
    IOpenRestoAvailabilityClient availabilityClient,
    IOpenRestoPrivateChannelClient privateChannelClient,
    IBotRequestContextAccessor requestContextAccessor) : IBotOperationRouter
{
    private readonly IOpenRestoAvailabilityClient _availabilityClient = availabilityClient;
    private readonly IOpenRestoPrivateChannelClient _privateChannelClient = privateChannelClient;
    private readonly IBotRequestContextAccessor _requestContextAccessor = requestContextAccessor;

    public async Task<BotOperationResponse> ExecuteAsync(BotOperationRequest request, CancellationToken cancellationToken)
    {
        BotRequestContext context = _requestContextAccessor.GetRequiredContext();

        return request.Operation switch
        {
            ReservationBotOperation.Availability => new BotOperationResponse
            {
                Operation = request.Operation,
                Availability = new AvailabilityLookupResponse
                {
                    Result = await _availabilityClient.LookupAsync(request.Availability!, context.CorrelationId, cancellationToken)
                }
            },
            ReservationBotOperation.Create => new BotOperationResponse
            {
                Operation = request.Operation,
                Reservation = new ReservationRecordResponse
                {
                    Result = await _privateChannelClient.CreateReservationAsync(request.Create!, context, cancellationToken)
                }
            },
            ReservationBotOperation.List => new BotOperationResponse
            {
                Operation = request.Operation,
                Reservations = new ReservationListResponse
                {
                    Result = await _privateChannelClient.ListReservationsAsync(context, cancellationToken)
                }
            },
            ReservationBotOperation.Detail => new BotOperationResponse
            {
                Operation = request.Operation,
                Reservation = new ReservationRecordResponse
                {
                    Result = await _privateChannelClient.GetReservationAsync(request.Detail!.ReservationId, context, cancellationToken)
                }
            },
            ReservationBotOperation.Update => new BotOperationResponse
            {
                Operation = request.Operation,
                Reservation = new ReservationRecordResponse
                {
                    Result = await _privateChannelClient.UpdateReservationAsync(request.Update!, context, cancellationToken)
                }
            },
            ReservationBotOperation.Cancel => new BotOperationResponse
            {
                Operation = request.Operation,
                Cancellation = new ReservationActionResponse
                {
                    Result = await _privateChannelClient.CancelReservationAsync(request.Cancel!, context, cancellationToken)
                }
            },
            ReservationBotOperation.OccasionCatalog => new BotOperationResponse
            {
                Operation = request.Operation,
                OccasionCatalog = new OccasionCatalogResponse
                {
                    Result = await _privateChannelClient.GetOccasionCatalogAsync(request.OccasionCatalog!.RestaurantId, context, cancellationToken)
                }
            },
            ReservationBotOperation.Handoff => new BotOperationResponse
            {
                Operation = request.Operation,
                Handoff = new HandoffRecordResponse
                {
                    Result = await _privateChannelClient.CreateHandoffAsync(request.Handoff!, context, cancellationToken)
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Operation, "La operación solicitada no existe.")
        };
    }
}
