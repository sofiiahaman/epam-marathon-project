using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SubmitGroupWithIds(
    string groupReservationId,
    string reservationId,
    string? bearerToken = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var response = await new GroupsApi(actor.UsingTheApi()).SubmitGroupAsync(
            groupReservationId,
            new SubmitGroupReservationRequest(reservationId),
            bearerToken);
        await actor.TheLastResponse().CaptureAsync(response);

        if (response.Ok)
            actor.Remember(actor.TheLastResponse().Json<IReadOnlyList<GroupSubmitItem>>());
    }
}
