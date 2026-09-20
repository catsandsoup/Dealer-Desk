using FlaUI.Core.AutomationElements;
using TextBox = FlaUI.Core.AutomationElements.TextBox;
using Button = FlaUI.Core.AutomationElements.Button;

namespace QuotingEngine.UITests.PageObjects;

public class SettingsPage : BasePage
{
    public SettingsPage(Window window) : base(window)
    {
    }

    public TextBox? CompanyNameBox => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("CompanyNameBox")), System.TimeSpan.FromSeconds(5)).Result?.AsTextBox();
    public Button? SaveBtn => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("SaveBtn")), System.TimeSpan.FromSeconds(5)).Result?.AsButton();
    public Button? CancelBtn => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("CancelBtn")), System.TimeSpan.FromSeconds(5)).Result?.AsButton();
}
