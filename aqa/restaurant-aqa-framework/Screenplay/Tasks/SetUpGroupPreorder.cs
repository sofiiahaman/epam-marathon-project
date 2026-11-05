using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SetUpGroupPreorder(bool withGuest, bool withDish = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var api = actor.UsingTheApi();
        var auth = new AuthApi(api);
        var credentials = SampleDataFactory.RegisterRequest();

        var registration = await auth.RegisterAsync(credentials);
        if (!registration.Ok)
            throw new InvalidOperationException($"Registering group owner failed: {await registration.ReadErrorMessageAsync()}");

        var signIn = await auth.SignInAsync(new SignInRequest(credentials.Email, credentials.Password));
        if (!signIn.Ok)
            throw new InvalidOperationException($"Signing in group owner failed: {await signIn.ReadErrorMessageAsync()}");

        var token = (await signIn.ReadAsAsync<SignInResponse>()).IdToken;
        var tableId = Settings.Current.TestTableId
            ?? throw new InvalidOperationException("TESTTABLE_ID is required for group pre-order scenarios.");

        var reservationApi = new ReservationsApi(api);
        var reservationResponse = await reservationApi.CreateReservationAsync(
            SampleDataFactory.CreateReservationRequest(tableId, SampleDataFactory.ReservationStartTime(), guestCount: 3),
            token);
        if (!reservationResponse.Ok)
            throw new InvalidOperationException($"Creating group reservation failed: {await reservationResponse.ReadErrorMessageAsync()}");

        var reservation = await reservationResponse.ReadAsAsync<ReservationResponse>();
        if (string.IsNullOrWhiteSpace(reservation.GroupReservationId))
            throw new InvalidOperationException($"Reservation did not create a group reservation: {await reservationResponse.TextAsync()}");

        actor.Remember(credentials);
        actor.Remember(token, "group-owner-token");
        actor.Remember(reservation);
        actor.Recall<CleanupScope>().TrackReservation(reservation.Id, reservationApi, token);

        if (!withGuest)
            return;

        var guestApi = new GuestSessionsApi(api);
        var guestResponse = await guestApi.CreateSessionAsync(
            SampleDataFactory.CreateGuestSessionRequest(reservation.GroupReservationId));
        if (!guestResponse.Ok)
            throw new InvalidOperationException($"Creating guest session failed: {await guestResponse.ReadErrorMessageAsync()}");

        var guest = await guestResponse.ReadAsAsync<CreateGuestSessionResponse>();
        actor.Remember(guest);
        actor.Recall<CleanupScope>().TrackGuestSession(
            reservation.GroupReservationId,
            guest.SessionId,
            new GroupsApi(api),
            token);

        if (!withDish)
            return;

        var dishes = await new DishesApi(api).GetDishesAsync();
        if (!dishes.Ok)
            throw new InvalidOperationException($"Loading dishes failed: {await dishes.ReadErrorMessageAsync()}");

        var dishList = await dishes.ReadAsAsync<IReadOnlyList<Api.Dtos.Dishes.DishListResponse>>();
        if (dishList.Count == 0)
            throw new InvalidOperationException("At least one dish is required for group pre-order scenarios.");

        var dish = dishList[0];
        var addDish = await guestApi.AddDishAsync(
            guest.SessionId,
            SampleDataFactory.CreateGuestSessionDishRequest(dish.Id, 2),
            guest.SessionToken);
        if (!addDish.Ok)
            throw new InvalidOperationException($"Adding guest dish failed: {await addDish.ReadErrorMessageAsync()}");

        actor.Remember(dish);
    }
}
