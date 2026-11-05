using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Ui;

public sealed class GroupPreorderPage(IPage page) : BasePage(page)
{
    private ILocator Root => Page.Locator("section.group-preorder");
    public ILocator Participants => Root.Locator(".participant");
    public ILocator EmptyState => Root.Locator(".group-preorder__message");
    public ILocator Total => Root.Locator(".group-preorder__summary");
    public ILocator Error => Root.Locator("[role='alert']");

    public Task RefreshAsync() =>
        Root.GetByRole(AriaRole.Button, new() { Name = "Оновити" }).ClickAsync();

    public Task OpenAsync() =>
        Page.Locator("button").Filter(new() { HasText = "Group pre-order" }).First.ClickAsync();

    public ILocator Participant(string name) =>
        Participants.Filter(new() { HasText = name });
}
