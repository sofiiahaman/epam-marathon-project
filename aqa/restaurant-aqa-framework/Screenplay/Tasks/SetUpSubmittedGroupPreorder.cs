using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Seeding;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SetUpSubmittedGroupPreorder(
    int guestCount = 1,
    bool submitGuest = true,
    bool addDish = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        await actor.AttemptsToAsync(new SetUpGroupPreorder(withGuest: false));
        var api = actor.UsingTheApi();
        var reservation = actor.Recall<ReservationResponse>();
        var ownerToken = actor.Recall<string>("group-owner-token");
        var guestApi = new GuestSessionsApi(api);
        if (!addDish)
        {
            await CreateGuestsAsync(actor, guestApi, new GroupsApi(api), reservation, ownerToken, guestCount, submitGuest, null);
            return;
        }

        var dishesResponse = await new DishesApi(api).GetDishesAsync();
        var dishes = await dishesResponse.ReadAsAsync<IReadOnlyList<Api.Dtos.Dishes.DishListResponse>>();
        if (addDish && dishes.Count == 0)
            throw new InvalidOperationException("At least one dish is required for group submit scenarios.");

        await CreateGuestsAsync(actor, guestApi, new GroupsApi(api), reservation, ownerToken, guestCount, submitGuest, dishes[0]);
        actor.Remember(dishes[0]);
    }

    private static async Task CreateGuestsAsync(
        Actor actor,
        GuestSessionsApi guestApi,
        GroupsApi groupsApi,
        ReservationResponse reservation,
        string ownerToken,
        int guestCount,
        bool submitGuest,
        Api.Dtos.Dishes.DishListResponse? dish)
    {
        for (var index = 0; index < guestCount; index++)
        {
            var guestResponse = await guestApi.CreateSessionAsync(
                SampleDataFactory.CreateGuestSessionRequest(reservation.GroupReservationId!));
            if (!guestResponse.Ok)
                throw new InvalidOperationException($"Creating guest session failed: {await guestResponse.ReadErrorMessageAsync()}");

            var guest = await guestResponse.ReadAsAsync<CreateGuestSessionResponse>();
            actor.Remember(guest);
            actor.Recall<CleanupScope>().TrackGuestSession(
                reservation.GroupReservationId!,
                guest.SessionId,
                groupsApi,
                ownerToken);

            if (dish is not null)
            {
                var addResponse = await guestApi.AddDishAsync(
                    guest.SessionId,
                    SampleDataFactory.CreateGuestSessionDishRequest(dish.Id, index + 1),
                    guest.SessionToken);
                if (!addResponse.Ok)
                    throw new InvalidOperationException($"Adding guest dish failed: {await addResponse.ReadErrorMessageAsync()}");
            }

            if (submitGuest)
            {
                var submitResponse = await guestApi.SubmitCartAsync(guest.SessionId, guest.SessionToken);
                if (!submitResponse.Ok)
                    throw new InvalidOperationException($"Submitting guest cart failed: {await submitResponse.ReadErrorMessageAsync()}");
            }
        }
    }
}
