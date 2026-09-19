using System;
using NUnit.Framework;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace QuotingEngine.UITests;

public class DealerDeskE2ETests
{
    private const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
    // Replace with the actual AppId of your deployed WinUI 3 app or the path to the .exe
    private const string AppId = @"C:\Users\monty\Documents\Dealer Desk\QuotingEngine.UI\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\QuotingEngine.UI.exe"; 
    
    protected static WindowsDriver<WindowsElement> session;

    [SetUp]
    public void Setup()
    {
        if (session == null)
        {
            var appiumOptions = new AppiumOptions();
            appiumOptions.AddAdditionalCapability("app", AppId);
            appiumOptions.AddAdditionalCapability("deviceName", "WindowsPC");
            
            session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), appiumOptions);
            
            Assert.IsNotNull(session);
            session.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (session != null)
        {
            session.Quit();
            session = null;
        }
    }

    [Test]
    public void StartApp_NavigateToSettings_VerifyNoHang()
    {
        // Example test checking that navigating to settings doesn't crash or hang
        var settingsButton = session.FindElementByAccessibilityId("SettingsNavViewItem");
        Assert.IsNotNull(settingsButton, "Settings button should be present");
        settingsButton.Click();
        
        var companyNameBox = session.FindElementByAccessibilityId("CompanyNameBox");
        Assert.IsNotNull(companyNameBox, "Settings page did not load correctly");
    }
}
