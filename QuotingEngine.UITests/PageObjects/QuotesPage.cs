using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using System.Threading;
using TextBox = FlaUI.Core.AutomationElements.TextBox;
using Button = FlaUI.Core.AutomationElements.Button;

namespace QuotingEngine.UITests.PageObjects;

public class QuotesPage : BasePage
{
    public QuotesPage(Window window) : base(window)
    {
    }

    public Button? AddRowBtn => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("AddRowBtn")), System.TimeSpan.FromSeconds(5)).Result?.AsButton();
    public TextBox? CustomerNameBox => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("CustomerNameBox")), System.TimeSpan.FromSeconds(5)).Result?.AsTextBox();
    public Grid? WorksheetGrid => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("WorksheetGrid")), System.TimeSpan.FromSeconds(5)).Result?.AsGrid();
    
    // Command Bar
    public Button? NewQuoteBtn => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("NewOrderBtn")), System.TimeSpan.FromSeconds(5)).Result?.AsButton();
    public Button? UndoBtn => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("UndoBtn")), System.TimeSpan.FromSeconds(5)).Result?.AsButton();
    public Button? DeleteDraftBtn => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("DeleteDraftBtn")), System.TimeSpan.FromSeconds(5)).Result?.AsButton();
    public Button? LockAndPrintBtn => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("LockQuoteBtn")), System.TimeSpan.FromSeconds(5)).Result?.AsButton();
    public FlaUI.Core.AutomationElements.AutomationElement? SettlementTotalText => FlaUI.Core.Tools.Retry.WhileNull(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("SettlementTotalText")), System.TimeSpan.FromSeconds(5)).Result;

    public void AddCustomItem(string weight, string purity)
    {
        AddRowBtn!.Invoke();
        Thread.Sleep(500);

        FlaUI.Core.AutomationElements.AutomationElement[] dataItems = new FlaUI.Core.AutomationElements.AutomationElement[0];
        for (int i = 0; i < 10; i++)
        {
            dataItems = WorksheetGrid.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataItem));
            if (dataItems.Length > 0) break;
            System.Threading.Thread.Sleep(500);
        }

        if (dataItems.Length == 0) return; // Prevent crash, just exit or throw
        
        var lastRow = dataItems.Last();
        var cells = lastRow.FindAllDescendants(cf => cf.ByClassName("DataGridCell"));
        
        for (int i = 0; i < cells.Length; i++)
        {
            try { Console.WriteLine($"Cell {i}: Name='{cells[i].Name}'"); } catch { }
        }
        
        if (cells.Length > 4)
        {
            // Enter weight (Gross Wt is typically cell index 3)
            var wtCell = cells[3];
            if (wtCell.Patterns.Invoke.IsSupported)
            {
                wtCell.Patterns.Invoke.Pattern.Invoke();
                Thread.Sleep(200);
                var edits = lastRow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
                if (edits.Length > 0)
                {
                    edits[0].AsTextBox().Text = weight;
                    Thread.Sleep(100);
                }
            }
            
            // Enter purity (Purity is typically cell index 4)
            var purityCell = cells[4];
            if (purityCell.Patterns.Invoke.IsSupported)
            {
                purityCell.Patterns.Invoke.Pattern.Invoke();
                Thread.Sleep(200);
                var edits = lastRow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
                if (edits.Length > 0)
                {
                    edits[0].AsTextBox().Text = purity;
                    Thread.Sleep(100);
                }
            }
        }
        
        // Force commit of the last edited cell by focusing the Add Row button
        AddRowBtn?.Focus();
        Thread.Sleep(200);
    }

    public string GetLastRowWeight()
    {
        FlaUI.Core.AutomationElements.AutomationElement[] dataItems = new FlaUI.Core.AutomationElements.AutomationElement[0];
        
        // Retry loop to wait for DataGrid rows to realize in the UIAutomation tree
        for (int i = 0; i < 10; i++)
        {
            dataItems = WorksheetGrid.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataItem));
            if (dataItems.Length > 0) break;
            System.Threading.Thread.Sleep(500); // Wait 500ms and try again
        }
        
        if (dataItems.Length == 0)
        {
            var dump = string.Join(" | ", WorksheetGrid.FindAllDescendants().Select(x => $"{x.ControlType}: '{x.Name}'"));
            throw new System.Exception($"[DEBUG_ABORT] WorksheetGrid has 0 DataItem descendants! Dump: {dump}");
        }

        var lastRow = dataItems.Last();
        var weightBox = lastRow.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit).And(cf.ByName("WeightInputText")).Or(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit)));
        return weightBox?.AsTextBox().Text ?? string.Empty;
    }

    public string GetLastRowPurity()
    {
        var dataItems = WorksheetGrid.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataItem));
        if (dataItems.Length == 0) return string.Empty;

        var lastRow = dataItems.Last();
        // Since purity is usually the second textbox or we can find it by name if available
        var purityBox = lastRow.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit).And(cf.ByName("PurityInputText")));
        if (purityBox == null)
        {
            // Fallback, get all edits and take the second one (Weight is 1st, Purity is 2nd)
            var edits = lastRow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
            if (edits.Length > 1) purityBox = edits[1];
        }
        
        return purityBox?.AsTextBox().Text ?? string.Empty;
    }
}
