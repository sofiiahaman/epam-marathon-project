using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class DeleteGroupOrder : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var reservation = actor.Recall<ReservationResponse>();
        var response = await new OrdersApi(actor.UsingTheApi()).DeleteOrderAsync(
            reservation.Id,
            actor.Recall<string>("group-owner-token"));
        await actor.TheLastResponse().CaptureAsync(response);
        if (!response.Ok)
            throw new InvalidOperationException($"Deleting the order failed: {await response.ReadErrorMessageAsync()}");
    }
}
