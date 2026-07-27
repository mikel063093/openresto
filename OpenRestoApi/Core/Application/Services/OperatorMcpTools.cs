using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenRestoApi.Core.Application.DTOs;

namespace OpenRestoApi.Core.Application.Services;

[McpServerToolType]
public sealed class OperatorMcpTools(
    OperatorAvailabilityService operatorAvailabilityService,
    OperatorReservationService operatorReservationService)
{
    private readonly OperatorAvailabilityService _operatorAvailabilityService = operatorAvailabilityService;
    private readonly OperatorReservationService _operatorReservationService = operatorReservationService;

    [McpServerTool(Name = "operator_get_availability", Title = "Get operator-scoped availability", ReadOnly = true, UseStructuredContent = true)]
    [Description("Returns reservation availability for a restaurant within the authenticated operator scope.")]
    public Task<AvailabilityResponseDto> GetAvailabilityAsync(
        [Description("Restaurant id within the operator scope.")] int restaurantId,
        [Description("Requested booking start in ISO-8601 format.")] DateTime date,
        [Description("Party size.")] int seats) =>
        _operatorAvailabilityService.GetAvailabilityAsync(restaurantId, date, seats);

    [McpServerTool(Name = "operator_create_reservation", Title = "Create operator reservation", Idempotent = false, UseStructuredContent = true)]
    [Description("Creates a reservation owned by the authenticated operator.")]
    public Task<BookingDto> CreateReservationAsync(OperatorReservationCreateRequestDto request) =>
        _operatorReservationService.CreateAsync(request.RestaurantId, request);

    [McpServerTool(Name = "operator_list_reservations", Title = "List owned reservations", ReadOnly = true, UseStructuredContent = true)]
    [Description("Lists reservations owned by the authenticated operator.")]
    public Task<List<BookingDto>> ListReservationsAsync() =>
        _operatorReservationService.ListOwnAsync();

    [McpServerTool(Name = "operator_get_reservation", Title = "Get owned reservation", ReadOnly = true, UseStructuredContent = true)]
    [Description("Gets a reservation owned by the authenticated operator.")]
    public async Task<BookingDto> GetReservationAsync(
        [Description("Reservation id.")] int reservationId) =>
        await _operatorReservationService.GetOwnOrThrowAsync(reservationId);

    [McpServerTool(Name = "operator_update_reservation", Title = "Update owned reservation", Idempotent = false, UseStructuredContent = true)]
    [Description("Updates a reservation owned by the authenticated operator.")]
    public Task<BookingDto> UpdateReservationAsync(
        [Description("Reservation id.")] int reservationId,
        OperatorReservationUpdateRequestDto request) =>
        _operatorReservationService.UpdateOwnAsync(reservationId, request);

    [McpServerTool(Name = "operator_cancel_reservation", Title = "Cancel owned reservation", Idempotent = false, UseStructuredContent = true)]
    [Description("Cancels a reservation owned by the authenticated operator.")]
    public Task<OperatorReservationActionResultDto> CancelReservationAsync(
        [Description("Reservation id.")] int reservationId) =>
        _operatorReservationService.CancelOwnAsync(reservationId);

    [McpServerTool(Name = "operator_escalate_reservation", Title = "Escalate owned reservation", Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Persists an escalation audit record and notifies the internal admin channel.")]
    public Task<OperatorReservationActionResultDto> EscalateReservationAsync(
        [Description("Reservation id.")] int reservationId,
        OperatorReservationEscalationRequestDto request) =>
        _operatorReservationService.EscalateOwnAsync(reservationId, request);
}
