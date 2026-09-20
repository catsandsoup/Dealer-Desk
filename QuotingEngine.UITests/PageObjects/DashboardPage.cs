using FlaUI.Core.AutomationElements;
using TextBox = FlaUI.Core.AutomationElements.TextBox;
using Button = FlaUI.Core.AutomationElements.Button;

namespace QuotingEngine.UITests.PageObjects;

public class DashboardPage : BasePage
{
    public DashboardPage(Window window) : base(window)
    {
    }

    public TextBox? SearchBox => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("SearchBox")), System.TimeSpan.FromSeconds(5)).Result?.AsTextBox();
    public Button? SearchBtn => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("SearchBtn")), System.TimeSpan.FromSeconds(5)).Result?.AsButton();
    public AutomationElement? QuotesList => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("QuotesList")), System.TimeSpan.FromSeconds(5)).Result;
}
