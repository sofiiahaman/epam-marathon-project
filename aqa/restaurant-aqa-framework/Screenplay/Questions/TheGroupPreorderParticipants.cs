using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

public sealed class TheGroupPreorderParticipants : IQuestion<int>
{
    public Task<int> AnsweredByAsync(Actor actor) =>
        new GroupPreorderPage(actor.LookingAtThePage()).Participants.CountAsync();
}

public sealed class TheGroupPreorderTotal : IQuestion<string>
{
    public async Task<string> AnsweredByAsync(Actor actor) =>
        (await new GroupPreorderPage(actor.LookingAtThePage()).Total.InnerTextAsync()).Trim();
}

public sealed class TheGroupParticipant : IQuestion<bool>
{
    private readonly string _name;

    public TheGroupParticipant(string name) => _name = name;

    public Task<bool> AnsweredByAsync(Actor actor) =>
        new GroupPreorderPage(actor.LookingAtThePage()).Participant(_name).IsVisibleAsync();
}
