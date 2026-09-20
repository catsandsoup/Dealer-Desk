using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace QuotingEngine.UI;

public sealed partial class ShellWindow : Window
{
    public ShellWindow()
    {
        this.InitializeComponent();

        // Win11 Modernization
        this.SystemBackdrop = new MicaBackdrop();
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);
        this.AppWindow.SetIcon("Assets\\AppIcon.ico");
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to default page
        NavView.SelectedItem = NavView.MenuItems[0];
        NavFrame.Navigate(typeof(DashboardPage));
        
        // Add global keyboard shortcut hook for F2 Spot Freeze
        if (this.Content is UIElement rootElement)
        {
            rootElement.PreviewKeyDown += RootElement_PreviewKeyDown;
        }
    }
    
    private void RootElement_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.F2)
        {
            var viewModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<QuotingEngine.UI.ViewModels.MainWindowViewModel>(QuotingEngine_UI.App.Host?.Services!);
            if (viewModel != null)
            {
                viewModel.ToggleSpotFreezeCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else
        {
            var navItemTag = args.SelectedItemContainer.Tag.ToString();
            
            if (navItemTag == "DashboardPage")
            {
                NavFrame.Navigate(typeof(DashboardPage));
            }
            else if (navItemTag == "QuotesPage")
            {
                NavFrame.Navigate(typeof(QuotesPage));
            }
        }
    }

    public void NavigateToQuotes()
    {
        foreach (var item in NavView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag.ToString() == "QuotesPage")
            {
                NavView.SelectedItem = navItem;
                break;
            }
        }
    }
}
