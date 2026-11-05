using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class GetGroupSessions(string groupReservationId, string? bearerToken = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var response = await new GroupsApi(actor.UsingTheApi())
            .GetSessionsAsync(groupReservationId, bearerToken);
        await actor.TheLastResponse().CaptureAsync(response);

        if (response.Ok)
            actor.Remember(actor.TheLastResponse().Json<GroupParticipantsResponse>());
    }
}

public sealed class GetGroupSessionsWithoutAuthorization(string groupReservationId) : ITask
{
    public Task PerformAsAsync(Actor actor) =>
        new GetGroupSessions(groupReservationId).PerformAsAsync(actor);
}
