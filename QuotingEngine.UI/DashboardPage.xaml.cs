using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using QuotingEngine.Infrastructure.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using QuotingEngine.Core.Models;

using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;

namespace QuotingEngine.UI;

public sealed partial class DashboardPage : Page
{
    private ObservableCollection<Quote> _quotes = new();
    private readonly QuotingEngine.Core.Api.SpotPriceClient? _spotClient;
    
    private static string DbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DealerDesk",
        "quoting_engine.db");
    
    public DashboardPage()
    {
        _spotClient = QuotingEngine_UI.App.Host?.Services.GetService<QuotingEngine.Core.Api.SpotPriceClient>();
        this.InitializeComponent();
        
        QuotesList.ItemsSource = _quotes;
        _ = LoadRecentQuotesAsync();
    }
    
    private async void RefreshSpotBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_spotClient != null)
        {
            try
            {
                // Disable button briefly to prevent spam
                RefreshSpotBtn.IsEnabled = false;
                await _spotClient.RefreshNow();
            }
            finally
            {
                RefreshSpotBtn.IsEnabled = true;
            }
        }
    }

    private async System.Threading.Tasks.Task LoadRecentQuotesAsync()
    {
        try
        {
            using var db = new AppDbContext(DbPath);
            
            var recent = await db.Quotes
                .Include(q => q.Items)
                .OrderByDescending(q => q.CreatedAtUtc)
                .Take(50)
                .ToListAsync();
                
            _quotes.Clear();
            foreach (var q in recent)
            {
                if (q.IsExpired())
                {
                    q.Status = QuoteStatus.Expired;
                    await db.SaveChangesAsync();
                }
                _quotes.Add(q);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Load error: {ex}");
        }
    }

    private void SearchBtn_Click(object sender, RoutedEventArgs e)
    {
        _ = PerformSearchAsync();
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            _ = PerformSearchAsync();
        }
    }

    private async System.Threading.Tasks.Task PerformSearchAsync()
    {
        try
        {
            var term = SearchBox.Text?.Trim().ToUpper() ?? "";
            using var db = new AppDbContext(DbPath);
            
            var query = db.Quotes.Include(q => q.Items).AsQueryable();
            
            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(q => 
                    q.ReferenceId.ToUpper().Contains(term) || 
                    q.CustomerName.ToUpper().Contains(term));
            }
            
            var results = await query.OrderByDescending(q => q.CreatedAtUtc).Take(50).ToListAsync();
            
            _quotes.Clear();
            foreach (var q in results)
            {
                if (q.IsExpired())
                {
                    q.Status = QuoteStatus.Expired;
                    await db.SaveChangesAsync();
                }
                _quotes.Add(q);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Search error: {ex}");
        }
    }

    private async void DeleteQuoteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.Button btn && btn.DataContext is Quote quote)
        {
            Microsoft.UI.Xaml.Controls.ContentDialog dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Delete Quote?",
                Content = $"Are you sure you want to delete quote {quote.ReferenceId} for {quote.CustomerName}? This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                try
                {
                    using var db = new AppDbContext(DbPath);
                    db.Quotes.Remove(quote);
                    db.SaveChanges();
                    
                    _quotes.Remove(quote);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Dashboard] Delete error: {ex}");
                }
            }
        }
    }

    private void EditQuoteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.Button btn && btn.DataContext is Quote quote)
        {
            var viewModel = QuotingEngine_UI.App.Host?.Services.GetService<QuotingEngine.UI.ViewModels.MainWindowViewModel>();
            if (viewModel != null)
            {
                viewModel.LoadQuote(quote);
                
                // Navigate to QuotesPage (assuming the shell is a NavigationView in ShellWindow)
                var shellWindow = QuotingEngine_UI.App.CurrentWindow as QuotingEngine.UI.ShellWindow;
                if (shellWindow != null)
                {
                    shellWindow.NavigateToQuotes();
                }
            }
        }
    }
}
