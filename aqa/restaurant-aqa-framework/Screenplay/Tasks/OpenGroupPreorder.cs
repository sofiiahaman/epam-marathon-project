using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class OpenGroupPreorder : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new ReservationDashboardPage(actor.LookingAtThePage());
        await page.NavigateAsync();
        await page.Reservation(0).OpenGroupPreorderAsync();
        await actor.LookingAtThePage().WaitForSelectorAsync(".group-preorder");
    }
}
