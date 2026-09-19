using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace QuotingEngine.UI;

public sealed partial class TearOutWindow : Window
{
    public TearOutWindow()
    {
        this.InitializeComponent();
        
        // Win11 Modernization
        this.SystemBackdrop = new MicaBackdrop();
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);
        this.AppWindow.SetIcon("Assets\\AppIcon.ico");
    }

    public void UpdateSpotPrice(decimal price, bool isFrozen)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            LiveSpotLargeText.Text = $"${price:F2}";
            
            if (isFrozen)
            {
                StatusLargeText.Text = "FROZEN";
                StatusLargeText.Foreground = new SolidColorBrush(Colors.Red);
                LiveSpotLargeText.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                StatusLargeText.Text = "ACTIVE";
                StatusLargeText.Foreground = new SolidColorBrush(Colors.LimeGreen);
                LiveSpotLargeText.Foreground = new SolidColorBrush(Colors.LimeGreen);
            }
        });
    }
}
