using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace QuotingEngine.UITests.PageObjects;

public class BasePage
{
    protected Window _window;

    public BasePage(Window window)
    {
        _window = window;
    }

    public void NavigateToQuotes()
    {
        var btn = FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByName("Quotes")), System.TimeSpan.FromSeconds(5)).Result;
        if (btn == null) throw new System.Exception("QuotesNavViewItem not found.");
        btn.Patterns.SelectionItem.Pattern.Select();
    }

    public void NavigateToSettings()
    {
        var btn = FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByName("Settings")), System.TimeSpan.FromSeconds(5)).Result;
        if (btn == null) throw new System.Exception("Settings nav item not found.");
        btn.Patterns.SelectionItem.Pattern.Select();
    }
    
    public void NavigateToDashboard()
    {
        var btn = FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByName("Inventory")), System.TimeSpan.FromSeconds(5)).Result;
        if (btn == null) throw new System.Exception("DashboardNavViewItem not found.");
        btn.Patterns.SelectionItem.Pattern.Select();
    }
}
