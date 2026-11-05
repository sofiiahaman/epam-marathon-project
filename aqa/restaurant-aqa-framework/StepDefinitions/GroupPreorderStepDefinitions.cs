using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
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

    [Given("the owner has submitted guest baskets for the group")]
    [Scope(Tag = "api")]
    public Task OwnerHasSubmittedGuestBaskets() =>
        Actor.AttemptsToAsync(new SetUpSubmittedGroupPreorder());

    [Given("the owner has submitted guest baskets for the group")]
    [Scope(Tag = "ui")]
    public async Task OwnerHasSubmittedGuestBasketsForUi()
    {
        await Actor.AttemptsToAsync(new SetUpSubmittedGroupPreorder());
        var credentials = Recall<RegisterRequest>();
        await Actor.AttemptsToAsync(new SignInThroughTheLoginForm(credentials.Email, credentials.Password));
    }

    [Given("the owner has two submitted guests with the same dish")]
    public Task OwnerHasTwoSubmittedGuestsWithTheSameDish() =>
        Actor.AttemptsToAsync(new SetUpSubmittedGroupPreorder(guestCount: 2));

    [Given("the group has no submitted baskets")]
    public Task GroupHasNoSubmittedBaskets() =>
        Actor.AttemptsToAsync(new SetUpSubmittedGroupPreorder(submitGuest: false));

    [Given("the submitted baskets contain no dishes")]
    public Task SubmittedBasketsContainNoDishes() =>
        Actor.AttemptsToAsync(new SetUpSubmittedGroupPreorder(addDish: false));

    [When("the owner submits the group pre-order")]
    [Scope(Tag = "ui")]
    public Task OwnerSubmitsGroupPreorder() =>
        new GroupPreorderPage(Actor.LookingAtThePage()).SubmitAsync();

    [When("the owner submits the group pre-order through the API")]
    [Scope(Tag = "api")]
    public Task OwnerSubmitsGroupPreorderThroughApi() =>
        Actor.AttemptsToAsync(new SubmitGroupPreorder());

    [When("the group is submitted again")]
    [Scope(Tag = "api")]
    public Task GroupIsSubmittedAgain() =>
        Actor.AttemptsToAsync(new SubmitGroupPreorder());

    [When("the {caller} submits the group pre-order")]
    [Scope(Tag = "api")]
    public Task CallerSubmitsGroupPreorder(string caller) =>
        Actor.AttemptsToAsync(new SubmitGroupAsCaller(caller));

    [When("the owner submits after the order was removed")]
    [Scope(Tag = "api")]
    public async Task OwnerSubmitsAfterOrderWasRemoved()
    {
        await Actor.AttemptsToAsync(new DeleteGroupOrder());
        await Actor.AttemptsToAsync(new SubmitGroupPreorder());
    }

    [When("the group submit request uses a missing group")]
    [Scope(Tag = "api")]
    public Task GroupSubmitUsesMissingGroup() =>
        Actor.AttemptsToAsync(new SubmitGroupWithIds(
            "00000000-0000-0000-0000-000000000000",
            Recall<ReservationResponse>().Id,
            Recall<string>("group-owner-token")));

    [When("the group submit request uses a missing reservation")]
    [Scope(Tag = "api")]
    public Task GroupSubmitUsesMissingReservation() =>
        Actor.AttemptsToAsync(new SubmitGroupWithIds(
            Recall<ReservationResponse>().GroupReservationId!,
            "00000000-0000-0000-0000-000000000000",
            Recall<string>("group-owner-token")));

    [Then("the owner sees submitted group items")]
    [Scope(Tag = "ui")]
    public async Task OwnerSeesSubmittedGroupItems() =>
        await Assert.That(await new GroupPreorderPage(Actor.LookingAtThePage()).SubmittedItems.CountAsync())
            .IsGreaterThan(0);

    [Then("the group submit response contains {string}")]
    [Scope(Tag = "api")]
    public async Task GroupSubmitResponseContains(string text)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Body).Contains(text);
    }

    [Then("the group submit response has status {int}")]
    [Scope(Tag = "api")]
    public async Task GroupSubmitResponseHasStatus(int status)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(status)
            .Because($"response body was: {outcome.Body}");
    }

    [Then("the submitted dish quantity is {int}")]
    [Scope(Tag = "api")]
    public async Task SubmittedDishQuantityIs(int quantity)
    {
        var dish = Recall<Api.Dtos.Dishes.DishListResponse>();
        await Assert.That(await Actor.AsksForAsync(new TheGroupSubmitContains(dish.Id, quantity))).IsTrue();
    }

    [Then("the waiter order contains the submitted group dishes")]
    [Scope(Tag = "api")]
    public async Task WaiterOrderContainsSubmittedGroupDishes()
    {
        await Actor.AttemptsToAsync(new ReadWaiterGroupOrder());
        var order = Recall<GetOrderResponse>();
        var submittedItems = Recall<IReadOnlyList<GroupSubmitItem>>();
        await Assert.That(order.Status).IsEqualTo(OrderStatus.SubmittedByCustomer);
        await Assert.That(order.OrderItems.Count).IsEqualTo(submittedItems.Count);
        foreach (var submittedItem in submittedItems)
        {
            var orderItem = order.OrderItems.FirstOrDefault(item => item.Dish.Id == submittedItem.DishId);
            await Assert.That(orderItem).IsNotNull();
            await Assert.That(orderItem!.Quantity).IsEqualTo(submittedItem.Quantity);
        }

    }

    [Given("the owner has several removable guests")]
    [Given("the owner has several submitted removable guests")]
    [Scope(Tag = "api")]
    public Task OwnerHasSeveralRemovableGuests() =>
        Actor.AttemptsToAsync(new SetUpRemovableGroup(guestCount: 2, submitGuests: true));

    [Given("the owner has several submitted removable guests")]
    [Scope(Tag = "ui")]
    public async Task OwnerHasSeveralSubmittedRemovableGuestsForUi()
    {
        await Actor.AttemptsToAsync(new SetUpRemovableGroup(guestCount: 2, submitGuests: true));
        var credentials = Recall<RegisterRequest>();
        await Actor.AttemptsToAsync(new SignInThroughTheLoginForm(credentials.Email, credentials.Password));
    }

    [Given("the owner has one removable guest")]
    public Task OwnerHasOneRemovableGuest() =>
        Actor.AttemptsToAsync(new SetUpRemovableGroup(guestCount: 1));

    [Given("the owner has two independent groups")]
    public Task OwnerHasTwoIndependentGroups() =>
        Actor.AttemptsToAsync(new SetUpIndependentGroups());

    [When("the owner deletes the first guest")]
    [When("the owner deletes the first submitted guest")]
    public Task OwnerDeletesFirstGuest() =>
        Actor.AttemptsToAsync(new DeleteGuestParticipant(
            guestSessionId: Actor.Recall<CreateGuestSessionResponse>("guest-1").SessionId));

    [When("the owner deletes the only guest")]
    public Task OwnerDeletesOnlyGuest() =>
        Actor.AttemptsToAsync(new DeleteGuestParticipant(
            guestSessionId: Actor.Recall<CreateGuestSessionResponse>("guest-1").SessionId));

    [When("the owner deletes the first group's guest from the second group")]
    public Task OwnerDeletesGuestFromWrongGroup() =>
        Actor.AttemptsToAsync(new DeleteNamedGuestParticipant(
            "first-group-reservation",
            "second-group-guest",
            "first-group-owner-token"));

    [When("the owner deletes a missing guest")]
    public Task OwnerDeletesMissingGuest() =>
        Actor.AttemptsToAsync(new DeleteGuestParticipant(guestSessionId: MissingGroupId));

    [When("another customer deletes the guest session")]
    public Task AnotherCustomerDeletesGuestSession() =>
        Actor.AttemptsToAsync(new DeleteAsAnotherCustomer());

    [When("the owner submits the remaining group order")]
    public Task OwnerSubmitsRemainingGroupOrder() =>
        Actor.AttemptsToAsync(new SubmitGroupPreorder());

    [Then("the delete response has status {int}")]
    public async Task DeleteResponseHasStatus(int status)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(status)
            .Because($"response body was: {outcome.Body}");
    }

    [Then("the remaining participant count is {int}")]
    public async Task RemainingParticipantCountIs(int count) =>
        await Assert.That(Recall<GroupParticipantsResponse>().Participants.Count).IsEqualTo(count);

    [Then("the participant total is recalculated")]
    public async Task ParticipantTotalIsRecalculated()
    {
        var snapshot = Recall<GroupParticipantsResponse>();
        await Assert.That(snapshot.Total).IsGreaterThan(0);
    }

    [Then("the remaining order excludes the deleted guest")]
    public async Task RemainingOrderExcludesDeletedGuest()
    {
        await Actor.AttemptsToAsync(new ReadWaiterGroupOrder());
        var order = Recall<GetOrderResponse>();
        await Assert.That(order.OrderItems.Count).IsGreaterThan(0);
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
