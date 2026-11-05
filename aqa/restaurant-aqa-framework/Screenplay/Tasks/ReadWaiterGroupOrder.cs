using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class ReadWaiterGroupOrder : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var reservation = actor.Recall<ReservationResponse>();
        var waiterToken = await new AuthApi(actor.UsingTheApi()).SignInAsWaiterAsync();
        var response = await new OrdersApi(actor.UsingTheApi()).GetOrderAsync(reservation.Id, waiterToken);
        await actor.TheLastResponse().CaptureAsync(response);

        if (response.Ok)
            actor.Remember(actor.TheLastResponse().Json<GetOrderResponse>());
    }
}
