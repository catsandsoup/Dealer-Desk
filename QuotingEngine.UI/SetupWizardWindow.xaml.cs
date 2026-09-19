using Microsoft.UI.Xaml;
using QuotingEngine.Core.Models;
using QuotingEngine.Infrastructure.Configuration;

using Microsoft.UI.Xaml.Media;

namespace QuotingEngine.UI;

public sealed partial class SetupWizardWindow : Window
{
    public SetupWizardWindow()
    {
        this.InitializeComponent();
        
        // Win11 Modernization
        this.SystemBackdrop = new MicaBackdrop();
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);
        this.AppWindow.SetIcon("Assets\\AppIcon.ico");
        
        LoadDefaults();
    }

    private void LoadDefaults()
    {
        var config = new DealerConfig();
        CompanyNameBox.Text = config.CompanyName;
        CompanyAbnBox.Text = config.CompanyAbn;
        TermsBox.Text = config.CustomTermsAndConditions;
        CurrencyBox.Text = config.BaseCurrency;
        TimezoneBox.Text = config.TimezoneId;
        ComPortBox.Text = config.ScaleComPort;
        BaudRateBox.Value = config.ScaleBaudRate;
        DefaultMarginBox.Value = (double)(config.DefaultScrapMargin * 100); // Convert decimal to percentage for UI
    }

    private async void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CompanyNameBox.Text) || string.IsNullOrWhiteSpace(ManagerPinBox.Password))
            {
                ErrorText.Text = "Company Name and Manager PIN are required.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            ErrorText.Visibility = Visibility.Collapsed;

            var config = new DealerConfig
            {
                IsConfigured = true,
                CompanyName = CompanyNameBox.Text.Trim(),
                CompanyAbn = CompanyAbnBox.Text.Trim(),
                CustomTermsAndConditions = TermsBox.Text.Trim(),
                BaseCurrency = CurrencyBox.Text.Trim().ToUpper(),
                TimezoneId = TimezoneBox.Text.Trim(),
                ScaleComPort = ComPortBox.Text.Trim().ToUpper(),
                ScaleBaudRate = (int)BaudRateBox.Value,
                ManagerPin = ManagerPinBox.Password,
                DefaultScrapMargin = (decimal)(DefaultMarginBox.Value / 100.0), // convert -5.0 to -0.05
                MetalPriceApiKey = MetalPriceApiKeyBox.Text.Trim(),
                MetalPriceCacheHours = (int)MetalPriceCacheHoursBox.Value
            };

            ConfigurationManager.Save(config);

            // Open ShellWindow and close this wizard
            // IMPORTANT: Activate ShellWindow BEFORE starting its background services,
            // so DispatcherQueue is fully wired and UI-thread marshaling works.
            var shellWindow = QuotingEngine_UI.App.Host?.Services.GetService(typeof(ShellWindow)) as ShellWindow;
            if (shellWindow != null)
            {
                shellWindow.Activate();
                this.Close();
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Failed to save: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine($"[SetupWizard] Save error: {ex}");
        }
    }
}
