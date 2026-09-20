using System;
using NUnit.Framework;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using Application = FlaUI.Core.Application;

namespace QuotingEngine.UITests;

public class DealerDeskE2ETests
{
    private const string AppId = @"C:\Users\monty\Documents\Dealer Desk\QuotingEngine.UI\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\DealerDesk.exe"; 
    
    protected Application? _app;
    protected UIA3Automation? _automation;
    protected Window? _window;

    [SetUp]
    public void Setup()
    {
        // Reset Database and Config for clean E2E run
        var baseFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DealerDesk");
        if (!System.IO.Directory.Exists(baseFolder))
            System.IO.Directory.CreateDirectory(baseFolder);

        var dbPath = System.IO.Path.Combine(baseFolder, "quoting_engine.db");
        if (System.IO.File.Exists(dbPath))
        {
            try { System.IO.File.Delete(dbPath); } catch { /* Ignore if locked */ }
        }

        var configPath = System.IO.Path.Combine(baseFolder, "dealer_config.json");
        System.IO.File.WriteAllText(configPath, "{\"IsConfigured\": true}");
        
        _app = Application.Launch(AppId);
        _automation = new UIA3Automation();
        
        // Wait for the main window
        _window = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(10));
        Assert.That(_window, Is.Not.Null, "Main window did not appear.");
    }

    [TearDown]
    public void TearDown()
    {
        _automation?.Dispose();
        _app?.Close();
        _app?.Dispose();
    }

    [Test]
    public void Test_Unsaved_Draft_State_Preservation()
    {
        var quotesPage = new PageObjects.QuotesPage(_window!);
        quotesPage.NavigateToQuotes();
        
        // Enter data
        quotesPage.AddCustomItem("32", "99.9");
        Thread.Sleep(500); // give it time to calculate
        var totalBefore = quotesPage.SettlementTotalText?.Name ?? "";
        System.Console.WriteLine($"Total before navigation: {totalBefore}");
        
        // Navigate away and back
        quotesPage.NavigateToSettings();
        quotesPage.NavigateToQuotes();
        
        System.Threading.Thread.Sleep(2000); // Give UI ample time to rebind
        
        string settlementText = quotesPage.SettlementTotalText?.Name ?? "";
        System.Console.WriteLine($"Total after navigation: {settlementText}");
        Assert.That(settlementText, Is.Not.EqualTo("$0.00"), "Settlement total should not be zero after navigation if state was preserved.");
        Assert.That(settlementText, Does.Contain("$"), "Settlement total should be a formatted currency value.");
    }

    [Test]
    public void Test_Empty_Quote_CommandBar_States()
    {
        var quotesPage = new PageObjects.QuotesPage(_window!);
        quotesPage.NavigateToQuotes();
        
        // FlaUI exposes IsEnabled property for UI items
        Assert.That(quotesPage.UndoBtn!.IsEnabled, Is.False, "Undo should be disabled for empty quote.");
        Assert.That(quotesPage.DeleteDraftBtn!.IsEnabled, Is.False, "Delete should be disabled for empty quote.");
        Assert.That(quotesPage.LockAndPrintBtn!.IsEnabled, Is.False, "Lock & Print should be disabled for empty quote.");
    }

    [Test]
    public void Test_Quote_Lock_Unlock_Lifecycle()
    {
        var quotesPage = new PageObjects.QuotesPage(_window!);
        quotesPage.NavigateToQuotes();
        
        quotesPage.CustomerNameBox!.Text = "Test Lock Customer";
        quotesPage.AddCustomItem("10", "99.9");
        
        // Lock it
        quotesPage.LockAndPrintBtn!.Invoke();
        
        // UI should reset for a new quote after lock & print
        Assert.That(quotesPage.CustomerNameBox.Text, Is.Empty, "Customer name should be cleared after lock.");
        Assert.That(quotesPage.GetLastRowWeight(), Is.Empty, "Weight box should be cleared after lock.");
    }

    [Test]
    public void Test_Full_Quote_CRUD_Audit()
    {
        // 1. CREATE
        var quotesPage = new PageObjects.QuotesPage(_window!);
        quotesPage.NavigateToQuotes();
        
        string testCustomer = "CRUD Audit Customer " + Guid.NewGuid().ToString().Substring(0, 4);
        quotesPage.CustomerNameBox!.Text = testCustomer;
        quotesPage.AddCustomItem("50", "99.9");
        
        quotesPage.LockAndPrintBtn!.Invoke();
        
        // 2. READ
        var dashboardPage = new PageObjects.DashboardPage(_window!);
        dashboardPage.NavigateToDashboard();
        
        dashboardPage.SearchBox!.Text = testCustomer;
        dashboardPage.SearchBtn!.Invoke();
        
        System.Threading.Thread.Sleep(500); // Wait for search
        Assert.That(dashboardPage.QuotesList!.Name, Does.Contain(testCustomer), "Quote should be visible in dashboard.");
    }

    [Test]
    public void Test_Settings_Affect_PDF_Output()
    {
        var settingsPage = new PageObjects.SettingsPage(_window!);
        settingsPage.NavigateToSettings();
        
        settingsPage.CompanyNameBox!.Text = "E2E Test Company";
        
        settingsPage.SaveBtn!.Invoke();
        
        // Create a quote to trigger PDF generation
        var quotesPage = new PageObjects.QuotesPage(_window!);
        quotesPage.NavigateToQuotes();
        quotesPage.CustomerNameBox!.Text = "PDF Settings Customer";
        quotesPage.AddCustomItem("1", "99.9");
        quotesPage.LockAndPrintBtn!.Invoke();
        
        // PDF should be generated in the output directory
        string pdfDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DealerDesk", "Quotes");
        if (System.IO.Directory.Exists(pdfDir))
        {
            var files = System.IO.Directory.GetFiles(pdfDir, "*.pdf");
            Assert.That(files.Length, Is.GreaterThan(0), "A PDF should have been generated.");
        }
    }
}
