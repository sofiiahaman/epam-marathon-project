using Restaurant_Aqa_Framework.Api.Dtos.Groups;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

public sealed class TheGroupSubmitItems : IQuestion<IReadOnlyList<GroupSubmitItem>>
{
    public Task<IReadOnlyList<GroupSubmitItem>> AnsweredByAsync(Actor actor) =>
        Task.FromResult(actor.Recall<IReadOnlyList<GroupSubmitItem>>());
}

public sealed class TheGroupSubmitContains(string dishId, int quantity) : IQuestion<bool>
{
    public Task<bool> AnsweredByAsync(Actor actor)
    {
        var item = actor.Recall<IReadOnlyList<GroupSubmitItem>>()
            .FirstOrDefault(candidate => candidate.DishId == dishId);
        return Task.FromResult(item?.Quantity == quantity);
    }
}
