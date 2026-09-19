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
}
