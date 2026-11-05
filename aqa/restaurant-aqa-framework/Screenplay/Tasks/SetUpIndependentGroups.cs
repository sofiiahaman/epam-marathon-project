using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SetUpIndependentGroups : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        await actor.AttemptsToAsync(new SetUpRemovableGroup(guestCount: 1));
        var firstReservation = actor.Recall<ReservationResponse>();
        var firstToken = actor.Recall<string>("group-owner-token");
        actor.Remember("first-group-reservation", firstReservation);
        actor.Remember("first-group-owner-token", firstToken);
        actor.Remember("first-group-guest", actor.Recall<CreateGuestSessionResponse>("guest-1"));

        await actor.AttemptsToAsync(new SetUpRemovableGroup(guestCount: 1));
        actor.Remember("second-group-reservation", actor.Recall<ReservationResponse>());
        actor.Remember("second-group-owner-token", actor.Recall<string>("group-owner-token"));
        actor.Remember("second-group-guest", actor.Recall<CreateGuestSessionResponse>("guest-1"));
    }
}
