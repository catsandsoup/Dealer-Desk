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
    private decimal _dealerMarginValue;
    private MarginType _marginType = MarginType.Percentage;
    private int _quantity = 1;
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

    public MarginType MarginType
    {
        get => _marginType;
        set
        {
            if (SetProperty(ref _marginType, value))
            {
                OnPropertyChanged(nameof(RawFiatValue));
                OnPropertyChanged(nameof(FinalFiatPrice));
            }
        }
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(RawFiatValue));
                OnPropertyChanged(nameof(FinalFiatPrice));
            }
        }
    }

    public decimal DealerMarginValue
    {
        get => _dealerMarginValue;
        set
        {
            if (SetProperty(ref _dealerMarginValue, value))
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
            // Base value is per unit, but if GrossWeightGrams represents the TOTAL weight across all units,
            // then FineWeightGrams * LiveSpotPricePerGram gives the TOTAL melt value already.
            // If GrossWeightGrams represents weight PER UNIT, then we must multiply by Quantity.
            // For standard operation, we will assume GrossWeightGrams is PER UNIT, 
            // so total base melt = (FineWeightGrams * LiveSpotPricePerGram) * Quantity.
            decimal totalMeltValue = (FineWeightGrams * LiveSpotPricePerGram) * Quantity;
            decimal totalMarginAmount = 0;
            
            switch (MarginType)
            {
                case MarginType.Percentage:
                    totalMarginAmount = totalMeltValue * DealerMarginValue;
                    break;
                case MarginType.FlatDollar:
                    // Flat dollar margin is applied PER UNIT (Quantity)
                    totalMarginAmount = DealerMarginValue * Quantity;
                    break;
            }

            decimal rawValue = totalMeltValue + totalMarginAmount;
            return Type == TransactionType.Buy ? rawValue : -rawValue;
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
