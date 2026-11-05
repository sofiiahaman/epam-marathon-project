using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SubmitGroupAsCaller(string caller) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var reservation = actor.Recall<ReservationResponse>();
        string? token = caller switch
        {
            "an anonymous user" => null,
            "a guest" => actor.Recall<CreateGuestSessionResponse>().SessionToken,
            "another customer" => await SignInAnotherCustomerAsync(actor),
            _ => throw new InvalidOperationException($"Unsupported caller '{caller}'."),
        };

        await actor.AttemptsToAsync(new SubmitGroupPreorder(
            token,
            reservation.Id));
    }

    private static async Task<string> SignInAnotherCustomerAsync(Actor actor)
    {
        var auth = new AuthApi(actor.UsingTheApi());
        var credentials = SampleDataFactory.RegisterRequest();
        var registration = await auth.RegisterAsync(credentials);
        if (!registration.Ok)
            throw new InvalidOperationException($"Registering second customer failed: {await registration.ReadErrorMessageAsync()}");

        var signIn = await auth.SignInAsync(new SignInRequest(credentials.Email, credentials.Password));
        if (!signIn.Ok)
            throw new InvalidOperationException($"Signing in second customer failed: {await signIn.ReadErrorMessageAsync()}");

        return (await signIn.ReadAsAsync<SignInResponse>()).IdToken;
    }
}
