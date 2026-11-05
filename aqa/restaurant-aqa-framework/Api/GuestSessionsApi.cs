using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;

namespace Restaurant_Aqa_Framework.Api;

public sealed class GuestSessionsApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> CreateSessionAsync(
        CreateGuestSessionRequest request,
        string? bearerToken = null) =>
        Api.PostAsync(Url("/guest/sessions"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> AddDishAsync(
        string guestSessionId,
        UpdateGuestSessionDishRequest request,
        string? guestSessionToken = null) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/dishes"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(guestSessionToken),
        });

    public Task<IAPIResponse> SubmitCartAsync(
        string guestSessionId,
        string? guestSessionToken = null) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/submit"), new APIRequestContextOptions
        {
            Headers = Headers(guestSessionToken),
        });
}
