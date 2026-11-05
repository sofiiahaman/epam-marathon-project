using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.StepDefinitions;

[Binding]
public sealed class GroupPreorderStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    private const string MissingGroupId = "00000000-0000-0000-0000-000000000000";

    [Given("the owner has opened the group pre-order")]
    [When("the owner opens the group pre-order")]
    [Scope(Tag = "ui")]
    public Task OwnerOpensGroupPreorder() =>
        Actor.AttemptsToAsync(new OpenGroupPreorder());

    [Given("the owner has a group pre-order with a guest")]
    public async Task OwnerHasGroupPreorderWithGuest()
    {
        await Actor.AttemptsToAsync(new SetUpGroupPreorder(withGuest: true));
        var credentials = Recall<RegisterRequest>();
        await Actor.AttemptsToAsync(new SignInThroughTheLoginForm(credentials.Email, credentials.Password));
        await Actor.AttemptsToAsync(new OpenGroupPreorder());
    }

    [Given("the owner has an empty group pre-order")]
    public async Task OwnerHasEmptyGroupPreorder()
    {
        await Actor.AttemptsToAsync(new SetUpGroupPreorder(withGuest: false));
        var credentials = Recall<RegisterRequest>();
        await Actor.AttemptsToAsync(new SignInThroughTheLoginForm(credentials.Email, credentials.Password));
        await Actor.AttemptsToAsync(new OpenGroupPreorder());
    }

    [Given("the owner has a group pre-order with a guest without dishes")]
    public async Task OwnerHasGroupPreorderWithGuestWithoutDishes()
    {
        await Actor.AttemptsToAsync(new SetUpGroupPreorder(withGuest: true, withDish: false));
        var credentials = Recall<RegisterRequest>();
        await Actor.AttemptsToAsync(new SignInThroughTheLoginForm(credentials.Email, credentials.Password));
        await Actor.AttemptsToAsync(new OpenGroupPreorder());
    }

    [When("the owner refreshes the group participants")]
    [Scope(Tag = "ui")]
    public Task OwnerRefreshesGroupParticipants() =>
        new GroupPreorderPage(Actor.LookingAtThePage()).RefreshAsync();

    [When("the guest changes the basket quantity to {int}")]
    public Task GuestChangesBasket(int quantity) =>
        Actor.AttemptsToAsync(new ChangeGuestBasket(quantity));

    [Then("the owner sees {int} group participant(s)")]
    [Scope(Tag = "ui")]
    public async Task OwnerSeesParticipants(int count) =>
        await Assert.That(await Actor.AsksForAsync(new TheGroupPreorderParticipants())).IsEqualTo(count);

    [Then("the owner sees participant {string}")]
    [Scope(Tag = "ui")]
    public async Task OwnerSeesParticipant(string name) =>
        await Assert.That(await Actor.AsksForAsync(new TheGroupParticipant(name))).IsTrue();

    [Then("the group response contains {string}")]
    [Scope(Tag = "api")]
    public async Task GroupResponseContains(string text)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Body).Contains(text);
    }

    [Then("the group request is refused with status {int}")]
    [Scope(Tag = "api")]
    public async Task GroupRequestIsRefused(int status)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(status).Because($"response body was: {outcome.Body}");
    }

    [Then("the group total is not zero")]
    [Scope(Tag = "api")]
    public async Task GroupTotalIsNotZero()
    {
        var snapshot = Recall<GroupParticipantsResponse>();
        await Assert.That(snapshot.Total).IsGreaterThan(0);
    }

    [When("the {caller} requests the group participants")]
    [Scope(Tag = "api")]
    public Task CallerRequestsGroupParticipants(string caller)
    {
        var token = caller switch
        {
            "anonymous user" or "guest" => null,
            _ => "invalid-caller-token",
        };

        return Actor.AttemptsToAsync(new GetGroupSessions(MissingGroupId, token));
    }

    [When("an anonymous user requests a missing group")]
    [Scope(Tag = "api")]
    public Task AnonymousUserRequestsMissingGroup() =>
        Actor.AttemptsToAsync(new GetGroupSessions(MissingGroupId));
}
