using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class DeleteNamedGuestParticipant(
    string groupReservationName,
    string guestName,
    string? tokenName = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var groupReservation = actor.Recall<ReservationResponse>(groupReservationName);
        var guest = actor.Recall<CreateGuestSessionResponse>(guestName);
        var token = tokenName is null ? actor.Recall<string>("group-owner-token") : actor.Recall<string>(tokenName);
        var response = await new GroupsApi(actor.UsingTheApi()).DeleteSessionAsync(
            groupReservation.GroupReservationId!,
            guest.SessionId,
            token);
        await actor.TheLastResponse().CaptureAsync(response);
        if (response.Ok)
            actor.Remember(actor.TheLastResponse().Json<GroupParticipantsResponse>());
    }
}
