using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace QuotingEngine.Core.Api;

public class SpotPriceEventArgs : EventArgs
{
    public decimal GoldSpotPrice { get; }

    public SpotPriceEventArgs(decimal goldSpotPrice)
    {
        GoldSpotPrice = goldSpotPrice;
    }
}

public class SpotPriceClient : IDisposable
{
    private decimal _currentPricePerGram = 0m;
    private CancellationTokenSource? _cts;
    private DateTime _lastTickUtc = DateTime.MinValue;
    private bool _isStale = true; // start as stale until we get data
    private bool _disposed;
    
    private readonly string _apiKey;
    private readonly string _baseCurrency;
    private readonly TimeSpan _cacheDuration;
    
    private readonly HttpClient _httpClient = new();
    
    private static string CachePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DealerDesk",
        "metal_prices_cache.json");

    /// <summary>
    /// Troy Ounce to Grams conversion factor.
    /// </summary>
    private const decimal TroyOunceToGrams = 31.1034768m;

    /// <summary>
    /// Fires on every valid spot price tick.
    /// </summary>
    public event EventHandler<SpotPriceEventArgs>? SpotPriceUpdated;

    /// <summary>
    /// Fires when the feed transitions between live and stale states.
    /// </summary>
    public event EventHandler<bool>? StaleStateChanged;

    public bool IsStale => _isStale;
    public DateTime LastTickUtc => _lastTickUtc;

    public SpotPriceClient(string apiKey, string baseCurrency, int cacheHours)
    {
        _apiKey = apiKey;
        _baseCurrency = string.IsNullOrWhiteSpace(baseCurrency) ? "AUD" : baseCurrency.ToUpper();
        _cacheDuration = TimeSpan.FromHours(cacheHours);
    }
    
    // For backwards compatibility before config loads
    public SpotPriceClient() : this("", "AUD", 24)
    {
    }

    public void StartConnecting()
    {
        if (_disposed) return;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        
        // Fire and forget the initial fetch loop
        Task.Run(() => FetchLoop(token), token);
    }
    
    public async Task RefreshNow()
    {
        await FetchFromApiAsync(force: true);
    }

    private async Task FetchLoop(CancellationToken token)
    {
        // First try to load from cache or API immediately
        await FetchFromApiAsync(force: false);
        
        // Then poll periodically (e.g., every hour, though we rely on CacheDuration to prevent spamming the API)
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), token); // Check every 15 mins
                if (!token.IsCancellationRequested)
                {
                    await FetchFromApiAsync(force: false);
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
    
    private async Task FetchFromApiAsync(bool force)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Debug.WriteLine("[SpotPriceClient] No API Key configured.");
                SetStale(true);
                return;
            }

            string json = "";
            bool fromCache = false;

            // Check Cache
            if (!force && File.Exists(CachePath))
            {
                var fileInfo = new FileInfo(CachePath);
                if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc < _cacheDuration)
                {
                    json = await File.ReadAllTextAsync(CachePath);
                    fromCache = true;
                    Debug.WriteLine("[SpotPriceClient] Loaded prices from cache.");
                }
            }

            // Fetch from API
            if (string.IsNullOrEmpty(json))
            {
                var url = $"https://api.metalpriceapi.com/v1/latest?api_key={_apiKey}&base={_baseCurrency}&currencies=XAU,XAG";
                Debug.WriteLine($"[SpotPriceClient] Fetching from API: {url}");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                json = await response.Content.ReadAsStringAsync();
                
                // Save to cache
                var dir = Path.GetDirectoryName(CachePath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(CachePath, json);
            }

            // Parse JSON
            var node = JsonNode.Parse(json);
            if (node != null && node["success"]?.GetValue<bool>() == true)
            {
                var rates = node["rates"];
                if (rates != null)
                {
                    // The API returns the rate as BaseCurrency -> XAU (e.g., AUDXAU) OR as XAU -> BaseCurrency depending on how it's formatted.
                    var rateKey = $"{_baseCurrency}XAU";
                    
                    decimal pricePerOz = 0m;
                    if (rates[rateKey] != null)
                    {
                        pricePerOz = rates[rateKey]!.GetValue<decimal>();
                    }
                    else if (rates["XAU"] != null)
                    {
                        // Fallback: If XAU = 0.000163, then 1 / XAU = price in base.
                        var xauRate = rates["XAU"]!.GetValue<decimal>();
                        if (xauRate > 0)
                            pricePerOz = 1m / xauRate;
                    }
                    
                    if (pricePerOz > 0)
                    {
                        _currentPricePerGram = pricePerOz / TroyOunceToGrams;
                        _lastTickUtc = DateTime.UtcNow;
                        SetStale(false);
                        SpotPriceUpdated?.Invoke(this, new SpotPriceEventArgs(_currentPricePerGram));
                        return;
                    }
                }
            }
            
            Debug.WriteLine("[SpotPriceClient] Failed to parse valid rates from JSON.");
            SetStale(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SpotPriceClient] Error fetching prices: {ex.Message}");
            SetStale(true);
        }
    }
    
    private void SetStale(bool isStale)
    {
        if (_isStale != isStale)
        {
            _isStale = isStale;
            StaleStateChanged?.Invoke(this, _isStale);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts?.Dispose();
        _httpClient.Dispose();
    }
}
