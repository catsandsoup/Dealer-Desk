using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuotingEngine.Core.Models;

public enum TransactionType
{
    Buy, // Dealer buys from customer (Dealer owes Customer = positive settlement)
    Sell // Dealer sells to customer (Customer owes Dealer = negative settlement)
}

public class LineItem : INotifyPropertyChanged
{
    public TransactionType[] AllTransactionTypes => new[] { TransactionType.Buy, TransactionType.Sell };

    private decimal _grossWeightGrams;
    private decimal _purityPercentage;
    private decimal _liveSpotPricePerGram;
    private decimal _dealerMarginPercentage;
    private string _description = string.Empty;
    private TransactionType _type = TransactionType.Buy;
    private byte[]? _itemPhoto;
    private decimal _storedSpotPriceG;
    private decimal _defaultPremiumPct;
    private decimal _managerOverrideSpreadDollar;

    public Guid Id { get; set; } = Guid.NewGuid();

    public byte[]? ItemPhoto
    {
        get => _itemPhoto;
        set => SetProperty(ref _itemPhoto, value);
    }

    public decimal StoredSpotPriceG
    {
        get => _storedSpotPriceG;
        set => SetProperty(ref _storedSpotPriceG, value);
    }

    public decimal DefaultPremiumPct
    {
        get => _defaultPremiumPct;
        set => SetProperty(ref _defaultPremiumPct, value);
    }

    public decimal ManagerOverrideSpreadDollar
    {
        get => _managerOverrideSpreadDollar;
        set => SetProperty(ref _managerOverrideSpreadDollar, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public TransactionType Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
            {
                OnPropertyChanged(nameof(RawFiatValue));
                OnPropertyChanged(nameof(FinalFiatPrice));
            }
        }
    }

    public decimal GrossWeightGrams
    {
        get => _grossWeightGrams;
        set
        {
            if (SetProperty(ref _grossWeightGrams, value))
            {
                OnPropertyChanged(nameof(FineWeightGrams));
                OnPropertyChanged(nameof(RawFiatValue));
                OnPropertyChanged(nameof(FinalFiatPrice));
            }
        }
    }

    public decimal PurityPercentage
    {
        get => _purityPercentage;
        set
        {
            if (SetProperty(ref _purityPercentage, value))
            {
                OnPropertyChanged(nameof(FineWeightGrams));
                OnPropertyChanged(nameof(RawFiatValue));
                OnPropertyChanged(nameof(FinalFiatPrice));
            }
        }
    }

    public decimal FineWeightGrams => GrossWeightGrams * PurityPercentage;

    public decimal LiveSpotPricePerGram
    {
        get => _liveSpotPricePerGram;
        set
        {
            if (SetProperty(ref _liveSpotPricePerGram, value))
            {
                OnPropertyChanged(nameof(RawFiatValue));
                OnPropertyChanged(nameof(FinalFiatPrice));
            }
        }
    }

    public decimal DealerMarginPercentage
    {
        get => _dealerMarginPercentage;
        set
        {
            if (SetProperty(ref _dealerMarginPercentage, value))
            {
                OnPropertyChanged(nameof(RawFiatValue));
                OnPropertyChanged(nameof(FinalFiatPrice));
            }
        }
    }

    /// <summary>
    /// Calculates the raw fiat value.
    /// If Buying, the value is positive (Dealer owes customer). Margin is usually a discount (negative).
    /// If Selling, the value is negative (Customer owes dealer). Margin is usually a premium (positive).
    /// </summary>
    public decimal RawFiatValue 
    {
        get 
        {
            var baseValue = FineWeightGrams * LiveSpotPricePerGram * (1 + DealerMarginPercentage);
            return Type == TransactionType.Buy ? baseValue : -baseValue;
        }
    }

    public decimal FinalFiatPrice => Math.Round(RawFiatValue, 2, MidpointRounding.ToEven);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
