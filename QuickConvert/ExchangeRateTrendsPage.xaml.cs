using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;

namespace QuickConvert
{
    public partial class ExchangeRateTrendsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient = new();
        private string _searchText;
        private string _baseCurrency = "USD";
        private DateTime _lastUpdated = DateTime.Now;
        private DateTime _comparisonDate;
        private TimeRange _selectedTimeRange = TimeRange.Week;
        private bool _isLoading;
        private CancellationTokenSource _debounceTokenSource;
        
        public new event PropertyChangedEventHandler PropertyChanged;
        
        public ObservableCollection<CurrencyTrend> ExchangeRates { get; } = new();
        private List<CurrencyTrend> _allRates = new();
        
        public ObservableCollection<string> PopularCurrencies { get; } = new(new[]
        {
            "USD", "EUR", "GBP", "JPY", "AUD", "CAD", "CHF", "CNY", "HKD", 
            "NZD", "SGD", "THB", "RUB", "INR"
        });
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    DebouncedFilterCurrencies();
                }
            }
        }
        
        public string BaseCurrency
        {
            get => _baseCurrency;
            set
            {
                if (SetProperty(ref _baseCurrency, value))
                {
                    _ = LoadExchangeRatesAsync();
                }
            }
        }

        public TimeRange SelectedTimeRange
        {
            get => _selectedTimeRange;
            set
            {
                if (SetProperty(ref _selectedTimeRange, value))
                {
                    _ = LoadExchangeRatesAsync();
                }
            }
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string TimeRangeText => $"{GetTimeRangeDescription(SelectedTimeRange)}";

        public string LastUpdatedText => $"Last updated: {_lastUpdated:dd MMM yyyy HH:mm}";

        public string ComparisonDateDescription => $"Change vs {_comparisonDate:dd MMM yyyy}";
        
        public ExchangeRateTrendsPage()
        {
            InitializeComponent();
            BindingContext = this;
            _ = LoadExchangeRatesAsync();
            
            _ = LoadAvailableCurrenciesAsync();
        }
        
        private async Task LoadAvailableCurrenciesAsync()
        {
            try
            {
                string url = "https://api.frankfurter.app/currencies";
                var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);
                
                if (response.ValueKind == JsonValueKind.Object)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var popularSet = new HashSet<string>(PopularCurrencies);
                        
                        foreach (var currency in response.EnumerateObject())
                        {
                            if (!popularSet.Contains(currency.Name))
                            {
                                PopularCurrencies.Add(currency.Name);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading available currencies: {ex.Message}");
            }
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadExchangeRatesAsync();
        }
        
        private async void DebouncedFilterCurrencies()
        {
            try
            {
                _debounceTokenSource?.Cancel();
                _debounceTokenSource = new CancellationTokenSource();
                var token = _debounceTokenSource.Token;
                
                await Task.Delay(150, token);
                
                await Task.Run(() => 
                {
                    if (token.IsCancellationRequested) return;
                    
                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        if (token.IsCancellationRequested) return;
                        FilterCurrencies();
                    });
                }, token);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during debounced filtering: {ex.Message}");
            }
        }
        
        private async Task LoadExchangeRatesAsync()
        {
            if (IsLoading) return;
            
            try
            {
                IsLoading = true;
                
                DateTime today = DateTime.Today;
                _comparisonDate = GetComparisonDate(today, SelectedTimeRange);
                
                string todayFormatted = today.ToString("yyyy-MM-dd");
                string comparisonDateFormatted = _comparisonDate.ToString("yyyy-MM-dd");
                
                var currentTask = _httpClient.GetFromJsonAsync<JsonElement>($"https://api.frankfurter.app/latest?from={BaseCurrency}");
                var historicalTask = _httpClient.GetFromJsonAsync<JsonElement>($"https://api.frankfurter.app/{comparisonDateFormatted}?from={BaseCurrency}");
                
                await Task.WhenAll(currentTask, historicalTask);
                
                var todayResponse = await currentTask;
                var historicalResponse = await historicalTask;
                
                if (todayResponse.TryGetProperty("rates", out var todayRates) && 
                    historicalResponse.TryGetProperty("rates", out var historicalRates) &&
                    todayResponse.TryGetProperty("date", out var dateProperty))
                {
                    _allRates.Clear();
                    
                    if (DateTime.TryParse(dateProperty.ToString(), out DateTime updateDate))
                    {
                        _lastUpdated = updateDate;
                        OnPropertyChanged(nameof(LastUpdatedText));
                    }
                    
                    OnPropertyChanged(nameof(ComparisonDateDescription));
                    OnPropertyChanged(nameof(TimeRangeText));
                    
                    foreach (var rate in todayRates.EnumerateObject())
                    {
                        if (rate.Name == BaseCurrency) continue;
                        
                        decimal todayRate = decimal.Parse(rate.Value.ToString());
                        decimal historicalRate = 1;
                        decimal change = 0;
                        
                        if (historicalRates.TryGetProperty(rate.Name, out var historicalValue))
                        {
                            historicalRate = decimal.Parse(historicalValue.ToString());
                            change = todayRate - historicalRate;
                        }
                        
                        decimal percentChange = 0;
                        if (historicalRate > 0)
                        {
                            percentChange = (change / historicalRate) * 100;
                        }
                        
                        _allRates.Add(new CurrencyTrend
                        {
                            CurrencyCode = rate.Name,
                            Flag = GetFlagForCurrency(rate.Name),
                            Rate = todayRate,
                            Change = change,
                            FormattedRate = $"{todayRate:F4} ",
                            FormattedChange = change >= 0 
                                ? $"+{change:F4} ({percentChange:F2}%)" 
                                : $"{change:F4} ({percentChange:F2}%)"
                        });
                    }
                    
                    FilterCurrencies();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading exchange rates: {ex.Message}");
                await DisplayAlert("Error", "Failed to load exchange rates. Please try again later.", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private DateTime GetComparisonDate(DateTime today, TimeRange range)
        {
            return range switch
            {
                TimeRange.Week => today.AddDays(-7),
                TimeRange.Month => today.AddMonths(-1),
                TimeRange.Year => today.AddYears(-1),
                _ => today.AddDays(-7)
            };
        }
        
        private string GetTimeRangeDescription(TimeRange range)
        {
            return range switch
            {
                TimeRange.Week => "1 week",
                TimeRange.Month => "1 month",
                TimeRange.Year => "1 year",
                _ => "1 week"
            };
        }
        
        private void FilterCurrencies()
        {
            ExchangeRates.Clear();
            
            var filteredItems = _allRates;
            
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filteredItems = _allRates.Where(x => 
                    x.CurrencyCode.ToLower().Contains(searchLower)).ToList();
            }
            
            foreach (var item in filteredItems)
            {
                ExchangeRates.Add(item);
            }
            
            Debug.WriteLine($"Filtered to {ExchangeRates.Count} currencies");
        }
        
        private string GetFlagForCurrency(string code)
        {
            return code switch
            {
                "USD" => "🇺🇸",
                "EUR" => "🇪🇺",
                "GBP" => "🇬🇧",
                "JPY" => "🇯🇵",
                "AUD" => "🇦🇺",
                "CAD" => "🇨🇦",
                "CHF" => "🇨🇭",
                "CNY" => "🇨🇳",
                "HKD" => "🇭🇰",
                "NZD" => "🇳🇿",
                "THB" => "🇹🇭",
                "SGD" => "🇸🇬",
                "RUB" => "🇷🇺",
                "INR" => "🇮🇳",
                "MXN" => "🇲🇽",
                "BRL" => "🇧🇷",
                "ZAR" => "🇿🇦",
                "TRY" => "🇹🇷",
                "KRW" => "🇰🇷",
                "IDR" => "🇮🇩",
                "MYR" => "🇲🇾",
                "PHP" => "🇵🇭",
                "SEK" => "🇸🇪",
                "NOK" => "🇳🇴",
                "DKK" => "🇩🇰",
                "PLN" => "🇵🇱",
                "CZK" => "🇨🇿",
                "HUF" => "🇭🇺",
                "ILS" => "🇮🇱",
                "RON" => "🇷🇴",
                "BGN" => "🇧🇬",
                "HRK" => "🇭🇷",
                "ISK" => "🇮🇸",
                "VND" => "🇻🇳",
                "EGP" => "🇪🇬",
                "SAR" => "🇸🇦",
                "AED" => "🇦🇪",
                "QAR" => "🇶🇦",
                "KWD" => "🇰🇼",
                "BHD" => "🇧🇭",
                "OMR" => "🇴🇲",
                "JOD" => "🇯🇴",
                "LBP" => "🇱🇧",
                "PKR" => "🇵🇰",
                "BDT" => "🇧🇩",
                "LKR" => "🇱🇰",
                "NPR" => "🇳🇵",
                "ARS" => "🇦🇷",
                "CLP" => "🇨🇱",
                "COP" => "🇨🇴",
                "PEN" => "🇵🇪",
                "UYU" => "🇺🇾",
                "UAH" => "🇺🇦",
                "NGN" => "🇳🇬",
                "GHS" => "🇬🇭",
                "KES" => "🇰🇪",
                "MAD" => "🇲🇦",
                "TWD" => "🇹🇼",
                "PYG" => "🇵🇾",
                "IQD" => "🇮🇶",
                "DZD" => "🇩🇿",
                "TND" => "🇹🇳",
                "AFN" => "🇦🇫",
                "BOB" => "🇧🇴",
                "CRC" => "🇨🇷",
                "DOP" => "🇩🇴",
                "XAF" => "🇨🇫", // Central African CFA franc
                "XOF" => "🇸🇳", // West African CFA franc
                "XCD" => "🇦🇬", // East Caribbean Dollar
                _ => "🏳️"
            };
        }
        
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadExchangeRatesAsync();
        }
        
        private async void OnCurrencySelected(object sender, EventArgs e)
        {
            if (sender is TapGestureRecognizer tapGesture && tapGesture.CommandParameter is CurrencyTrend currency)
            {
                string action = await DisplayActionSheet(
                    $"{currency.Flag} {currency.CurrencyCode} Options:", 
                    "Cancel", 
                    null, 
                    "Set as Base Currency", 
                    "Add to Bookmarks", 
                    "View Change Details");
                
                switch (action)
                {
                    case "Set as Base Currency":
                        BaseCurrency = currency.CurrencyCode;
                        break;
                    case "Add to Bookmarks":
                        if (!BookmarkStore.BookmarkedCurrencies.Contains(currency.CurrencyCode))
                            BookmarkStore.BookmarkedCurrencies.Add(currency.CurrencyCode);
                        await DisplayAlert("Success", $"{currency.CurrencyCode} added to bookmarks!", "OK");
                        break;
                    case "View Change Details":
                        await DisplayAlert($"{currency.CurrencyCode} vs {BaseCurrency}", 
                            $"Currency: {currency.CurrencyCode}\n" +
                            $"Current Rate: {currency.Rate:F4} {BaseCurrency}\n" +
                            $"Change ({GetTimeRangeDescription(SelectedTimeRange)}): {currency.FormattedChange}\n\n" +
                            $"Date Range: {_comparisonDate:yyyy-MM-dd} to {_lastUpdated:yyyy-MM-dd}", 
                            "Close");
                        break;
                }
            }
        }
        
        private void OnClearSearchClicked(object sender, EventArgs e)
        {
            SearchText = string.Empty;
        }
        
        private async void OnLoadMoreItems(object sender, EventArgs e)
        {
        }
        
        private async void OnChangeTimeRangeClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet(
                "Select Time Range for Comparison",
                "Cancel",
                null,
                "1 Week",
                "1 Month",
                "1 Year");
                
            switch (action)
            {
                case "1 Week":
                    SelectedTimeRange = TimeRange.Week;
                    break;
                case "1 Month":
                    SelectedTimeRange = TimeRange.Month;
                    break;
                case "1 Year":
                    SelectedTimeRange = TimeRange.Year;
                    break;
            }
        }
    }
    
    public enum TimeRange
    {
        Week,
        Month,
        Year
    }
    
    public class CurrencyTrend : INotifyPropertyChanged
    {
        private bool _isVisible = true;
        
        public string CurrencyCode { get; set; }
        public string Flag { get; set; }
        public decimal Rate { get; set; }
        public decimal Change { get; set; }
        public string FormattedRate { get; set; }
        public string FormattedChange { get; set; }
        
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ChangeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal change)
            {
                return change >= 0 ? Colors.Green : Colors.Red;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrWhiteSpace(stringValue);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
