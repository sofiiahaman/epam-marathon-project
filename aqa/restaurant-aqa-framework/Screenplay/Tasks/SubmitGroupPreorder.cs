using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SubmitGroupPreorder(string? bearerToken = null, string? reservationId = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var reservation = actor.Recall<ReservationResponse>();
        var groupId = reservation.GroupReservationId
            ?? throw new InvalidOperationException("The reservation has no group reservation.");
        var response = await new GroupsApi(actor.UsingTheApi()).SubmitGroupAsync(
            groupId,
            new SubmitGroupReservationRequest(reservationId ?? reservation.Id),
            bearerToken ?? actor.Recall<string>("group-owner-token"));

        await actor.TheLastResponse().CaptureAsync(response);
        if (response.Ok)
            actor.Remember(actor.TheLastResponse().Json<IReadOnlyList<GroupSubmitItem>>());
    }
}
