using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class DeleteAsAnotherCustomer : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var reservation = actor.Recall<ReservationResponse>();
        var auth = new AuthApi(actor.UsingTheApi());
        var credentials = SampleDataFactory.RegisterRequest();
        var registration = await auth.RegisterAsync(credentials);
        if (!registration.Ok)
            throw new InvalidOperationException($"Registering second customer failed: {await registration.ReadErrorMessageAsync()}");

        var signIn = await auth.SignInAsync(new SignInRequest(credentials.Email, credentials.Password));
        if (!signIn.Ok)
            throw new InvalidOperationException($"Signing in second customer failed: {await signIn.ReadErrorMessageAsync()}");

        var token = (await signIn.ReadAsAsync<SignInResponse>()).IdToken;
        var guest = actor.Recall<CreateGuestSessionResponse>("guest-1");
        var response = await new GroupsApi(actor.UsingTheApi()).DeleteSessionAsync(
            reservation.GroupReservationId!,
            guest.SessionId,
            token);
        await actor.TheLastResponse().CaptureAsync(response);
    }
}
