using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class DeleteGuestParticipant(
    string? groupReservationId = null,
    string? guestSessionId = null,
    string? bearerToken = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var reservation = actor.Recall<ReservationResponse>();
        var groupId = groupReservationId ?? reservation.GroupReservationId
            ?? throw new InvalidOperationException("The reservation has no group reservation.");
        var guest = guestSessionId is null
            ? actor.Recall<CreateGuestSessionResponse>()
            : null;
        var response = await new GroupsApi(actor.UsingTheApi()).DeleteSessionAsync(
            groupId,
            guestSessionId ?? guest!.SessionId,
            bearerToken ?? actor.Recall<string>("group-owner-token"));

        await actor.TheLastResponse().CaptureAsync(response);
        if (response.Ok)
            actor.Remember(actor.TheLastResponse().Json<GroupParticipantsResponse>());
    }
}
