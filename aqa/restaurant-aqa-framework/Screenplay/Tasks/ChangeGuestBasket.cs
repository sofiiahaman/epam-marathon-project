using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class ChangeGuestBasket(int quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<CreateGuestSessionResponse>();
        var dish = actor.Recall<DishListResponse>();
        var response = await new GuestSessionsApi(actor.UsingTheApi()).AddDishAsync(
            guest.SessionId,
            new UpdateGuestSessionDishRequest(dish.Id, quantity),
            guest.SessionToken);

        await actor.TheLastResponse().CaptureAsync(response);
        if (!response.Ok)
            throw new InvalidOperationException($"Changing guest basket failed: {await response.ReadErrorMessageAsync()}");
    }
}
