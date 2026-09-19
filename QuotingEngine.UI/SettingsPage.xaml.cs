using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuotingEngine.Core.Models;
using QuotingEngine.Infrastructure.Configuration;

namespace QuotingEngine.UI;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
        LoadDefaults();
    }

    private void LoadDefaults()
    {
        var config = ConfigurationManager.Load();
        CompanyNameBox.Text = config.CompanyName;
        CompanyAbnBox.Text = config.CompanyAbn;
        TermsBox.Text = config.CustomTermsAndConditions;
        CurrencyBox.Text = config.BaseCurrency;
        TimezoneBox.Text = config.TimezoneId;
        ComPortBox.Text = config.ScaleComPort;
        BaudRateBox.Value = config.ScaleBaudRate;
        ManagerPinBox.Password = config.ManagerPin;
        DefaultMarginBox.Value = (double)(config.DefaultScrapMargin * 100); 
        MetalPriceApiKeyBox.Text = config.MetalPriceApiKey;
        MetalPriceCacheHoursBox.Value = config.MetalPriceCacheHours;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        // For a page inside navigation, we can just navigate back
        if (this.Frame != null && this.Frame.CanGoBack)
        {
            this.Frame.GoBack();
        }
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
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

            var config = ConfigurationManager.Load();
            config.CompanyName = CompanyNameBox.Text.Trim();
            config.CompanyAbn = CompanyAbnBox.Text.Trim();
            config.CustomTermsAndConditions = TermsBox.Text.Trim();
            config.BaseCurrency = CurrencyBox.Text.Trim().ToUpper();
            config.TimezoneId = TimezoneBox.Text.Trim();
            config.ScaleComPort = ComPortBox.Text.Trim().ToUpper();
            config.ScaleBaudRate = (int)BaudRateBox.Value;
            config.ManagerPin = ManagerPinBox.Password;
            config.DefaultScrapMargin = (decimal)(DefaultMarginBox.Value / 100.0);
            config.MetalPriceApiKey = MetalPriceApiKeyBox.Text.Trim();
            config.MetalPriceCacheHours = (int)MetalPriceCacheHoursBox.Value;

            ConfigurationManager.Save(config);

            if (this.Frame != null && this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Failed to save: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private async void FactoryResetBtn_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "Factory Reset",
            Content = "This will permanently delete all configuration and historical quotes. Are you absolutely sure?",
            PrimaryButtonText = "Reset Everything",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Delete DB and config
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DealerDesk");
            
            try
            {
                if (Directory.Exists(appData))
                {
                    // Specifically delete quoting_engine.db and config.json
                    string dbPath = Path.Combine(appData, "quoting_engine.db");
                    if (File.Exists(dbPath)) File.Delete(dbPath);
                    
                    string configPath = Path.Combine(appData, "config.json");
                    if (File.Exists(configPath)) File.Delete(configPath);
                }

                // Swap back to onboarding wizard by relaunching the app essentially, or just launching SetupWizard and closing MainWindow
                var setupWizard = new SetupWizardWindow();
                setupWizard.Activate();
                
                
                // Get the ShellWindow to close it
                var shellWindow = QuotingEngine_UI.App.CurrentWindow;
                shellWindow?.Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Failed to reset: {ex.Message}";
                ErrorText.Visibility = Visibility.Visible;
            }
        }
    }
}
