using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using QuotingEngine.Core.Models;
using QuotingEngine.Core.Api;
using QuotingEngine.Core.Services;
using QuotingEngine.Infrastructure.Data;
using QuotingEngine.Infrastructure.Hardware;
using QuotingEngine.Infrastructure.Configuration;

namespace QuotingEngine.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly SpotPriceClient _spotClient;
    private readonly SerialPortReader _serialReader;
    private readonly PricingCalculator _calculator = new();
    
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    // The undo stack: stores a snapshot of the LineItems list.
    private readonly System.Collections.Generic.Stack<System.Collections.Generic.List<LineItem>> _undoStack = new();

    public ObservableCollection<LineItem> LineItems { get; set; } = new();
    public ObservableCollection<CatalogItem> CatalogItems { get; } = new();
    public ObservableCollection<string> AvailableAssayTools { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSpotPriceText))]
    private decimal _currentSpotPrice = 82.50m;

    public string CurrentSpotPriceText => $"${CurrentSpotPrice:F2}";

    [ObservableProperty]
    private bool _isSpotFrozen = false;

    [ObservableProperty]
    private string _customerName = "";

    [ObservableProperty]
    private string _idDocument = "";

    [ObservableProperty]
    private string _idNumber = "";
    
    [ObservableProperty]
    private CatalogItem? _selectedCatalogItem;

    [ObservableProperty]
    private string _statusText = "LIVE";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SettlementTotalText))]
    private decimal _settlementTotal;

    public string SettlementTotalText => $"${SettlementTotal:F2}";

    // A flag to determine if the spot price is stale
    public bool IsStale => _spotClient?.IsStale ?? false;

    public MainWindowViewModel(
        AppDbContext dbContext,
        SpotPriceClient spotClient,
        SerialPortReader serialReader)
    {
        _dbContext = dbContext;
        _spotClient = spotClient;
        _serialReader = serialReader;

        // Ensure database is initialized (this shouldn't block the UI in a real app, 
        // but for now we preserve the behavior pending async host startup).
        LoadCatalog();
        
        var config = ConfigurationManager.Load();
        foreach (var tool in config.AvailableAssayTools)
        {
            AvailableAssayTools.Add(tool);
        }

        LineItems.CollectionChanged += (s, e) => 
        {
            if (e.NewItems != null)
                foreach (LineItem item in e.NewItems)
                    item.PropertyChanged += Item_PropertyChanged;
            
            if (e.OldItems != null)
                foreach (LineItem item in e.OldItems)
                    item.PropertyChanged -= Item_PropertyChanged;
                    
            UpdateSettlementTotal();
        };

        // Wire up services
        _spotClient.SpotPriceUpdated += OnSpotPriceUpdated;
        _spotClient.StaleStateChanged += OnStaleStateChanged;
        
        _serialReader.DataReceived += OnSerialDataReceived;
    }

    private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateSettlementTotal();
    }

    private async void LoadCatalog()
    {
        try
        {
            var items = await System.Threading.Tasks.Task.Run(() => _dbContext.CatalogItems.ToList());
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var item in items)
                {
                    CatalogItems.Add(item);
                }
                if (CatalogItems.Any())
                {
                    SelectedCatalogItem = CatalogItems.First();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ViewModel] DB load error: {ex}");
        }
    }

    private void UpdateSettlementTotal()
    {
        var total = LineItems.Sum(x => x.FinalFiatPrice);
        SettlementTotal = total;
    }

    private void OnSpotPriceUpdated(object? sender, SpotPriceEventArgs e)
    {
        if (IsSpotFrozen) return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            CurrentSpotPrice = Math.Round(e.GoldSpotPrice, 2);
            
            // Update all unlocked lines
            foreach (var item in LineItems)
            {
                item.LiveSpotPricePerGram = CurrentSpotPrice;
            }
            
            UpdateSettlementTotal();
        });
    }

    private void OnStaleStateChanged(object? sender, bool isStale)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (isStale)
            {
                StatusText = "⚠ STALE DATA";
                OnPropertyChanged(nameof(IsStale));
            }
            else
            {
                if (!IsSpotFrozen)
                {
                    StatusText = "LIVE";
                }
                OnPropertyChanged(nameof(IsStale));
            }
        });
    }

    private void OnSerialDataReceived(object? sender, string data)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // Usually we'd bind this to the SelectedItem of the DataGrid, 
            // but for simplicity, if there's only one row or we pick the last one:
            var item = LineItems.LastOrDefault();
            if (item != null)
            {
                var input = data.Trim().ToLower();
                if (input.EndsWith("g") && decimal.TryParse(input.Replace("g", ""), out var weight))
                {
                    item.GrossWeightGrams = weight;
                }
                else if (decimal.TryParse(input, out var purity))
                {
                    item.PurityPercentage = purity;
                }
            }
        });
    }

    [RelayCommand]
    private void AddRow()
    {
        SaveUndoState();
        LineItems.Add(_calculator.CreateScrapItem(0, 0, CurrentSpotPrice, -0.05m)); // Default margin injected later ideally
    }

    [RelayCommand]
    private void AddCatalogItem()
    {
        if (SelectedCatalogItem is { } catItem)
        {
            SaveUndoState();
            var lineItem = new LineItem
            {
                Description = catItem.Name,
                GrossWeightGrams = catItem.GrossWeightGrams,
                PurityPercentage = catItem.PurityPercentage,
                MarginType = catItem.MarginType,
                DealerMarginValue = catItem.DefaultMarginValue,
                Quantity = 1,
                LiveSpotPricePerGram = CurrentSpotPrice,
                Type = TransactionType.Buy
            };
            LineItems.Add(lineItem);
        }
    }

    [RelayCommand]
    private void DeleteRow(LineItem? item)
    {
        if (item != null)
        {
            SaveUndoState();
            LineItems.Remove(item);
        }
    }

    [RelayCommand]
    private void ToggleSpotFreeze()
    {
        IsSpotFrozen = !IsSpotFrozen;
        StatusText = IsSpotFrozen ? "FROZEN" : "LIVE";
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var previousState = _undoStack.Pop();
            LineItems.Clear();
            foreach (var item in previousState)
            {
                LineItems.Add(item);
            }
        }
    }

    [RelayCommand]
    private void NewOrder()
    {
        SaveUndoState();
        LineItems.Clear();
        CustomerName = "";
        IdDocument = "";
        IdNumber = "";
        IsSpotFrozen = false;
        StatusText = "LIVE";
    }

    [RelayCommand]
    private void DeleteDraft()
    {
        // For a draft, it's essentially the same as NewOrder (clearing the screen).
        // If we had saved it to DB, we'd delete it from DB here.
        NewOrder();
    }

    private void SaveUndoState()
    {
        var snapshot = LineItems.Select(x => new LineItem
        {
            Description = x.Description,
            GrossWeightGrams = x.GrossWeightGrams,
            PurityPercentage = x.PurityPercentage,
            MarginType = x.MarginType,
            DealerMarginValue = x.DealerMarginValue,
            Quantity = x.Quantity,
            LiveSpotPricePerGram = x.LiveSpotPricePerGram,
            Type = x.Type
        }).ToList();
        
        _undoStack.Push(snapshot);
    }

    public void Dispose()
    {
        _spotClient.SpotPriceUpdated -= OnSpotPriceUpdated;
        _spotClient.StaleStateChanged -= OnStaleStateChanged;
        _serialReader.DataReceived -= OnSerialDataReceived;
    }
}
