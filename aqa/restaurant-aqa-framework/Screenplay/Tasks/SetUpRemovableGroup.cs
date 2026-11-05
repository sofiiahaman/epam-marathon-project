using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Seeding;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SetUpRemovableGroup(
    int guestCount = 2,
    bool submitGuests = false) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        await actor.AttemptsToAsync(new SetUpGroupPreorder(withGuest: false));
        var api = actor.UsingTheApi();
        var reservation = actor.Recall<ReservationResponse>();
        var ownerToken = actor.Recall<string>("group-owner-token");
        var dishesResponse = await new DishesApi(api).GetDishesAsync();
        var dishes = await dishesResponse.ReadAsAsync<IReadOnlyList<Api.Dtos.Dishes.DishListResponse>>();
        if (dishes.Count == 0)
            throw new InvalidOperationException("At least one dish is required for remove-participant scenarios.");

        var guestApi = new GuestSessionsApi(api);
        var groupsApi = new GroupsApi(api);
        for (var index = 0; index < guestCount; index++)
        {
            var response = await guestApi.CreateSessionAsync(
                SampleDataFactory.CreateGuestSessionRequest(reservation.GroupReservationId!));
            if (!response.Ok)
                throw new InvalidOperationException($"Creating guest session failed: {await response.ReadErrorMessageAsync()}");

            var guest = await response.ReadAsAsync<CreateGuestSessionResponse>();
            actor.Remember($"guest-{index + 1}", guest);
            actor.Recall<CleanupScope>().TrackGuestSession(
                reservation.GroupReservationId!, guest.SessionId, groupsApi, ownerToken);

            var addResponse = await guestApi.AddDishAsync(
                guest.SessionId,
                SampleDataFactory.CreateGuestSessionDishRequest(dishes[0].Id, index + 1),
                guest.SessionToken);
            if (!addResponse.Ok)
                throw new InvalidOperationException($"Adding guest dish failed: {await addResponse.ReadErrorMessageAsync()}");

            if (submitGuests)
            {
                var submitResponse = await guestApi.SubmitCartAsync(guest.SessionId, guest.SessionToken);
                if (!submitResponse.Ok)
                    throw new InvalidOperationException($"Submitting guest cart failed: {await submitResponse.ReadErrorMessageAsync()}");
            }
        }

        actor.Remember(dishes[0]);
        actor.Remember("guest-count", guestCount);
    }
}
