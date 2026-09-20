using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;
using QuotingEngine.Core.Models;
using QuotingEngine.Infrastructure.Documents;
using QuotingEngine.Infrastructure.Configuration;
using QuotingEngine.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace QuotingEngine.UI;

public sealed partial class QuotesPage : Page
{
    public MainWindowViewModel ViewModel { get; }
    
    private TearOutWindow? _tickerWindow;
    private readonly DealerConfig _config;

    public QuotesPage()
    {
        // Resolve ViewModel from global host
        ViewModel = QuotingEngine_UI.App.Host?.Services.GetService<MainWindowViewModel>()!;
        _config = QuotingEngine_UI.App.Host?.Services.GetService<DealerConfig>()!;
        
        this.InitializeComponent();
        this.DataContext = this;
        
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;

        // Setup keyboard hook for F2 at page level
        this.Loaded += (s, e) => {
            this.Focus(FocusState.Programmatic);
            
            // Force DataGrid UIAutomation tree to detach and reattach completely
            WorksheetGrid.Visibility = Visibility.Collapsed;
            WorksheetGrid.UpdateLayout();
            WorksheetGrid.Visibility = Visibility.Visible;
            WorksheetGrid.UpdateLayout();
            
            // Force DataGrid to realize items by triggering CollectionChanged asynchronously
            var items = ViewModel.LineItems.ToList();
            if (items.Count > 0)
            {
                ViewModel.LineItems.Clear();
                _ = DispatcherQueue.TryEnqueue(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(50); // Let UI process empty grid
                    foreach (var item in items)
                    {
                        ViewModel.LineItems.Add(item);
                    }
                });
            }
        };
        this.Unloaded += QuotesPage_Unloaded;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.CurrentSpotPrice) || e.PropertyName == nameof(ViewModel.IsSpotFrozen))
        {
            _tickerWindow?.UpdateSpotPrice(ViewModel.CurrentSpotPrice, ViewModel.IsSpotFrozen);
        }
    }

    private void QuotesPage_Unloaded(object sender, RoutedEventArgs args)
    {
        // Mitigate WinUI 3 infinite layout cycle bug on cached pages with DataGrid star sizing
        WorksheetGrid.Visibility = Visibility.Collapsed;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _tickerWindow?.Close();
    }

    // ──────────────────────────────────────────────
    //  Navigation & UI Events
    // ──────────────────────────────────────────────


    private void DeleteRowBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is LineItem item)
        {
            ViewModel.DeleteRowCommand.Execute(item);
        }
    }

    private async void CapturePhotoBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is LineItem item)
        {
            var dialog = new CameraCaptureDialog
            {
                XamlRoot = this.Content.XamlRoot
            };
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.CapturedPhotoBytes != null)
            {
                item.ItemPhoto = dialog.CapturedPhotoBytes;
                ViewModel.StatusText = "Photo captured successfully.";
            }
        }
    }

    private void TearOutTickerBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_tickerWindow == null)
        {
            _tickerWindow = new TearOutWindow();
            _tickerWindow.Closed += (s, args) => _tickerWindow = null;
        }
        
        _tickerWindow.Activate();
        _tickerWindow.UpdateSpotPrice(ViewModel.CurrentSpotPrice, ViewModel.IsSpotFrozen);
    }

    private void OnCustomerNameKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            WorksheetGrid.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }

    private void Content_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Z && 
                 Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            ViewModel.UndoCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void F2_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.ToggleSpotFreezeCommand.Execute(null);
        args.Handled = true;
    }

    // ──────────────────────────────────────────────
    //  Complex Dialog Logic
    // ──────────────────────────────────────────────

    private async void LockQuoteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.CustomerName))
        {
            ViewModel.StatusText = "⚠ Enter Customer Name";
            CustomerNameBox.Focus(FocusState.Programmatic);
            return;
        }

        if (ViewModel.IsStale)
        {
            ViewModel.StatusText = "⚠ CANNOT LOCK — STALE DATA";
            return;
        }

        if (!ViewModel.LineItems.Any())
        {
            ViewModel.StatusText = "⚠ Add items before locking";
            return;
        }

        var total = ViewModel.SettlementTotal;
        
        // AML/KYC Threshold check
        if (Math.Abs(total) > 10000m)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.IdDocument) || string.IsNullOrWhiteSpace(ViewModel.IdNumber))
            {
                ViewModel.StatusText = "⚠ AML limit exceeded. ID required.";
                return;
            }
        }

        // Manager Override check
        bool needsOverride = ViewModel.LineItems.Any(i => 
            (i.Type == TransactionType.Buy && i.DealerMarginValue > 0) || 
            (i.Type == TransactionType.Sell && i.DealerMarginValue < 0));
            
        if (needsOverride)
        {
            var pinBox = new PasswordBox { PlaceholderText = "PIN" };
            var dialog = new ContentDialog
            {
                Title = "Manager Override Required",
                Content = new StackPanel { Children = { new TextBlock { Text = "Unfavorable margins detected." }, pinBox } },
                PrimaryButtonText = "Approve",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary || pinBox.Password != _config.ManagerPin)
            {
                ViewModel.StatusText = "⚠ Manager override denied.";
                return;
            }
        }

        try
        {
            int graceHours = GracePeriodCombo.SelectedIndex switch
            {
                0 => 0,
                1 => 24,
                2 => 48,
                _ => 0
            };

            var quote = new Quote
            {
                CustomerName = ViewModel.CustomerName,
                CustomerIdDocument = ViewModel.IdDocument,
                CustomerIdNumber = ViewModel.IdNumber,
                Status = QuoteStatus.Locked,
                LockedAtUtc = DateTime.UtcNow,
                GracePeriodHours = graceHours,
                ExpiresAtUtc = graceHours == 0 ? null : DateTime.UtcNow.AddHours(graceHours),
                LockedSpotPricePerGram = ViewModel.CurrentSpotPrice,
                Items = ViewModel.LineItems.ToList()
            };

            var filePath = QuotingEngine.Infrastructure.Pdf.PdfGenerator.GenerateQuotePdf(quote, _config);
            var hash = QuotingEngine.Infrastructure.Pdf.PdfGenerator.ComputeFileHash(filePath);
            quote.PdfSha256Hash = hash;

            // Resolve DbContext to save
            var dbContext = QuotingEngine_UI.App.Host?.Services.GetRequiredService<QuotingEngine.Infrastructure.Data.AppDbContext>();
            if (dbContext != null)
            {
                dbContext.Quotes.Add(quote);
                dbContext.SaveChanges();
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PDF] Could not open viewer: {ex.Message}");
            }

            ViewModel.StatusText = $"QUOTE LOCKED: {quote.ReferenceId}";
            
            // Reset state
            ViewModel.LineItems.Clear();
            ViewModel.CustomerName = "";
            ViewModel.IdDocument = "";
            ViewModel.IdNumber = "";
        }
        catch (Exception ex)
        {
            ViewModel.StatusText = $"⚠ Lock failed: {ex.Message}";
            Debug.WriteLine($"[LockQuote] Error: {ex}");
        }
    }
}
