using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace QuickConvert
{
    public partial class ChartPage : ContentPage, INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient = new();
        private string _baseCurrency = "USD";
        private string _targetCurrency = "EUR";
        private DateTime _lastUpdated = DateTime.Now;
        private TimeRange _selectedTimeRange = TimeRange.Week;
        private ObservableCollection<ExchangeRateDataPoint> _chartData = new();
        private string _chartPathData = "M0,100 L100,100";
        private string _chartFillPathData = "M0,100 L100,100 L100,180 L0,180 Z";
        private decimal _currentValue = 0;
        private decimal _previousValue = 0;
        private decimal _maxValue = 0;
        private decimal _midValue = 0; 
        private decimal _minValue = 0;
        private bool _isDarkMode = false;
        
        public new event PropertyChangedEventHandler PropertyChanged;
        
        public ObservableCollection<string> AvailableCurrencies { get; } = new(new[]
        {
            "USD", "EUR", "GBP", "JPY", "AUD", "CAD", "CHF", "CNY", "HKD", 
            "NZD", "SGD", "THB", "RUB", "INR"
        });
        
        public ObservableCollection<ExchangeRateDataPoint> ChartData
        {
            get => _chartData;
            set => SetProperty(ref _chartData, value);
        }
        
        public string ChartPathData 
        {
            get => _chartPathData;
            set => SetProperty(ref _chartPathData, value);
        }
        
        public string ChartFillPathData
        {
            get => _chartFillPathData;
            set => SetProperty(ref _chartFillPathData, value);
        }
        
        public string BaseCurrency
        {
            get => _baseCurrency;
            set
            {
                if (SetProperty(ref _baseCurrency, value))
                {
                    _ = LoadExchangeRateHistoryAsync();
                }
            }
        }
        
        public string TargetCurrency
        {
            get => _targetCurrency;
            set
            {
                if (SetProperty(ref _targetCurrency, value))
                {
                    _ = LoadExchangeRateHistoryAsync();
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
                    _ = LoadExchangeRateHistoryAsync();
                    UpdateTimeRangeUI();
                }
            }
        }

        public string TimeRangeText => $"{GetTimeRangeDescription(SelectedTimeRange)}";
        public string LastUpdatedText => $"Last updated: {_lastUpdated:dd MMM HH:mm}";
        public string PairText => $"{BaseCurrency}/{TargetCurrency} Exchange Rate";
        
        public decimal HighValue { get; set; }
        public decimal LowValue { get; set; }
        public decimal AverageValue { get; set; }
        
        public string HighValueText => $"{HighValue:F4}";
        public string LowValueText => $"{LowValue:F4}";
        public string AverageValueText => $"{AverageValue:F4}";
        
        public decimal CurrentValue 
        { 
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }
        
        public decimal PreviousValue 
        { 
            get => _previousValue;
            set => SetProperty(ref _previousValue, value);
        }
        
        public string CurrentValueText => $"{CurrentValue:F4}";
        public string PreviousValueText => $"{PreviousValue:F4}";
        
        public decimal MaxValue 
        { 
            get => _maxValue;
            set => SetProperty(ref _maxValue, value);
        }
        
        public decimal MidValue 
        { 
            get => _midValue;
            set => SetProperty(ref _midValue, value);
        }
        
        public decimal MinValue 
        { 
            get => _minValue;
            set => SetProperty(ref _minValue, value);
        }
        
        public string MaxValueText => $"{MaxValue:F4}";
        public string MidValueText => $"{MidValue:F4}";
        public string MinValueText => $"{MinValue:F4}";
        
        public string WeekBackground => SelectedTimeRange == TimeRange.Week ? "#5F259F" : "#F3F0FF";
        public string WeekTextColor => SelectedTimeRange == TimeRange.Week ? "White" : "#5F259F";
        
        public string MonthBackground => SelectedTimeRange == TimeRange.Month ? "#5F259F" : "#F3F0FF";
        public string MonthTextColor => SelectedTimeRange == TimeRange.Month ? "White" : "#5F259F";
        
        public string YearBackground => SelectedTimeRange == TimeRange.Year ? "#5F259F" : "#F3F0FF";
        public string YearTextColor => SelectedTimeRange == TimeRange.Year ? "White" : "#5F259F";
        
        public string CurrentTrend { get; set; }
        
        public string TrendColor => CurrentTrend?.StartsWith("+") == true ? "#29A745" : "#DC3545";
        
        public ChartPage()
        {
            InitializeComponent();
            BindingContext = this;
            
            _isDarkMode = Preferences.Get("IsDarkMode", false);
            
            _ = LoadAvailableCurrenciesAsync();
            _ = LoadExchangeRateHistoryAsync();
        }
        
        private async Task LoadAvailableCurrenciesAsync()
        {
            try
            {
                string url = "https://api.frankfurter.app/currencies";
                var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);
                
                if (response.ValueKind == JsonValueKind.Object)
                {
                    var existingCurrencies = new HashSet<string>(AvailableCurrencies);
                    
                    foreach (var currency in response.EnumerateObject())
                    {
                        if (!existingCurrencies.Contains(currency.Name))
                        {
                            AvailableCurrencies.Add(currency.Name);
                        }
                    }
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
            
            
            _ = LoadExchangeRateHistoryAsync();
        }
        
        private DateTime GetStartDate(DateTime endDate, TimeRange range)
        {
            return range switch
            {
                TimeRange.Week => endDate.AddDays(-7),
                TimeRange.Month => endDate.AddMonths(-1),
                TimeRange.Year => endDate.AddYears(-1),
                _ => endDate.AddDays(-7)
            };
        }
        
        private async Task LoadExchangeRateHistoryAsync()
        {
            try
            {
                DateTime endDate = DateTime.Now;
                DateTime startDate = GetStartDate(endDate, SelectedTimeRange);
                
                ChartData.Clear();
                
                string startDateFormatted = startDate.ToString("yyyy-MM-dd");
                string endDateFormatted = endDate.ToString("yyyy-MM-dd");
                
                string url = $"https://api.frankfurter.app/{startDateFormatted}..{endDateFormatted}?from={BaseCurrency}&to={TargetCurrency}";
                Debug.WriteLine($"Fetching exchange rates from: {url}");
                
                var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);
                Debug.WriteLine($"API response received: {response.ToString().Substring(0, Math.Min(response.ToString().Length, 100))}...");
                
                if (response.TryGetProperty("rates", out var rates))
                {
                    foreach (var rate in rates.EnumerateObject())
                    {
                        if (DateTime.TryParse(rate.Name, out DateTime date))
                        {
                            if (rate.Value.TryGetProperty(TargetCurrency, out var rateValue) && 
                                decimal.TryParse(rateValue.ToString(), out decimal value))
                            {
                                ChartData.Add(new ExchangeRateDataPoint
                                {
                                    Date = date,
                                    Value = value
                                });
                            }
                        }
                    }
                    
                    var orderedData = ChartData.OrderBy(p => p.Date).ToList();
                    ChartData = new ObservableCollection<ExchangeRateDataPoint>(orderedData);
                    
                    List<decimal> values = ChartData.Select(p => p.Value).ToList();
                    if (values.Any())
                    {
                        HighValue = values.Max();
                        LowValue = values.Min();
                        AverageValue = values.Average();
                        
                        if (ChartData.Count >= 2)
                        {
                            var oldestValue = ChartData.OrderBy(p => p.Date).First().Value;
                            var newestValue = ChartData.OrderByDescending(p => p.Date).First().Value;
                            decimal percentChange = ((newestValue - oldestValue) / oldestValue) * 100;
                            
                            CurrentTrend = percentChange >= 0 
                                ? $"+{percentChange:F2}% ⬆" 
                                : $"{percentChange:F2}% ⬇";
                        
                            CurrentValue = newestValue;
                            if (ChartData.Count > 1)
                            {
                                var secondNewest = ChartData.OrderByDescending(p => p.Date).Skip(1).First().Value;
                                PreviousValue = secondNewest;
                            }
                            else
                            {
                                PreviousValue = oldestValue;
                            }
                        }
                        else
                        {
                            CurrentTrend = "0.00% -";
                        
                            if (ChartData.Count > 0)
                            {
                                CurrentValue = ChartData[0].Value;
                                PreviousValue = ChartData[0].Value;
                            }
                        }
                    }
                    else
                    {
                        CurrentTrend = "0.00% -";
                    
                        if (ChartData.Count > 0)
                        {
                            CurrentValue = ChartData[0].Value;
                            PreviousValue = ChartData[0].Value;
                        }
                    }
                }
                
                _lastUpdated = DateTime.Now;
                
                OnPropertyChanged(nameof(HighValueText));
                OnPropertyChanged(nameof(LowValueText));
                OnPropertyChanged(nameof(AverageValueText));
                OnPropertyChanged(nameof(LastUpdatedText));
                OnPropertyChanged(nameof(CurrentTrend));
                OnPropertyChanged(nameof(PairText));
                OnPropertyChanged(nameof(CurrentValueText));
                OnPropertyChanged(nameof(PreviousValueText));
                OnPropertyChanged(nameof(TrendColor));
                OnPropertyChanged(nameof(MaxValueText));
                OnPropertyChanged(nameof(MidValueText));
                OnPropertyChanged(nameof(MinValueText));
                
                UpdateChart();
            
                UpdateCurrencyFlags();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading exchange rate history: {ex.Message}");
                await DisplayAlert("Error", "Failed to load exchange rate history. Please try again later.", "OK");
            }
        }
        
        private void UpdateChart()
        {
            if (ChartData.Count < 2)
            {
                ChartPathData = "M0,100 L100,100";
                ChartFillPathData = "M0,100 L100,100 L100,180 L0,180 Z";
                Debug.WriteLine("Not enough data points for chart, using default line");
                return;
            }
            
            try
            {
                var orderedData = ChartData.OrderBy(p => p.Date).ToList();
                
                decimal minValue = orderedData.Min(p => p.Value);
                decimal maxValue = orderedData.Max(p => p.Value);
                
                MinValue = minValue;
                MaxValue = maxValue;
                MidValue = (minValue + maxValue) / 2;
                
                decimal displayMin = minValue;
                decimal displayMax = maxValue;
                decimal range = displayMax - displayMin;
                
                if (range < 0.0001m)
                {
                    decimal midPoint = (displayMax + displayMin) / 2;
                    midPoint = midPoint == 0 ? 0.0001m : midPoint;
                    decimal padding = Math.Max(0.0001m, midPoint * 0.01m); // 1% padding, minimum 0.0001
                    displayMin = midPoint - padding;
                    displayMax = midPoint + padding;
                    range = displayMax - displayMin;
                    Debug.WriteLine($"Adjusted display range for flat line: {displayMin} to {displayMax}");
                }
                else
                {
                    decimal padding = range * 0.05m;
                    displayMin -= padding;
                    displayMax += padding;
                    range = displayMax - displayMin;
                }
                
                if (range <= 0) 
                {
                    range = 0.0001m;
                    Debug.WriteLine("Zero range detected, using minimum value");
                }
                
                StringBuilder pathBuilder = new StringBuilder();
                StringBuilder fillPathBuilder = new StringBuilder();
                
                const double chartHeight = 180;
                
                double x0 = 0;
                double y0 = chartHeight - (chartHeight * (double)((orderedData[0].Value - displayMin) / range));
                y0 = Math.Max(5, Math.Min(chartHeight - 5, y0));
                
                pathBuilder.Append($"M{x0},{y0}");
                fillPathBuilder.Append($"M{x0},{y0}");
                
                for (int i = 1; i < orderedData.Count; i++)
                {
                    double x = (i * 100.0) / (orderedData.Count - 1);
                    double y = chartHeight - (chartHeight * (double)((orderedData[i].Value - displayMin) / range));
                    y = Math.Max(5, Math.Min(chartHeight - 5, y));
                    
                    pathBuilder.Append($"L{x},{y}");
                }
                
                string pathData = pathBuilder.ToString();
                fillPathBuilder.Append(pathData.Substring(1));
                fillPathBuilder.Append($"L100,{chartHeight}L0,{chartHeight}Z");
                
                Debug.WriteLine($"Generated chart path: {pathData}");
                ChartPathData = pathData;
                ChartFillPathData = fillPathBuilder.ToString();
                
                CurrentValue = orderedData.Last().Value;
                PreviousValue = orderedData.Count > 1 ? orderedData[orderedData.Count - 2].Value : orderedData[0].Value;
                
                for (int i = 1; i < orderedData.Count; i++)
                {
                    decimal previousValue = orderedData[i - 1].Value;
                    decimal currentValue = orderedData[i].Value;
                    
                    if (previousValue != 0)
                    {
                        decimal change = (currentValue - previousValue) / previousValue;
                        orderedData[i].Change = change;
                    }
                }
                
                HighValue = maxValue;
                LowValue = minValue;
                AverageValue = orderedData.Average(p => p.Value);
                
                this.Dispatcher.Dispatch(() => {
                    try
                    {
                        if (orderedData.Count > 0)
                        {
                            var firstDate = orderedData.First().Date.ToString("MMM dd");
                            var lastDate = orderedData.Last().Date.ToString("MMM dd");
                            
                            var startLabel = FindByName("startDateLabel") as Label;
                            if (startLabel != null)
                                startLabel.Text = firstDate;
                            
                            var endLabel = FindByName("endDateLabel") as Label;
                            if (endLabel != null)
                                endLabel.Text = lastDate;
                        }
                        
                        OnPropertyChanged(nameof(HighValueText));
                        OnPropertyChanged(nameof(LowValueText));
                        OnPropertyChanged(nameof(AverageValueText));
                        OnPropertyChanged(nameof(MaxValueText));
                        OnPropertyChanged(nameof(MidValueText));
                        OnPropertyChanged(nameof(MinValueText));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating data points: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating chart: {ex.Message}");
                ChartPathData = "M10,100L50,50L90,75";
                ChartFillPathData = "M10,100L50,50L90,75L90,180L10,180Z";
            }
        }
        
        private string GetTimeRangeDescription(TimeRange range)
        {
            return range switch
            {
                TimeRange.Week => "1w",
                TimeRange.Month => "1m",
                TimeRange.Year => "1y",
                _ => "1w"
            };
        }
        
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("Refreshing chart data...");
                await this.Dispatcher.DispatchAsync(async () => 
                {
                    await LoadExchangeRateHistoryAsync();
                    
                    OnPropertyChanged(nameof(ChartPathData));
                    OnPropertyChanged(nameof(ChartFillPathData));
                    
                    _lastUpdated = DateTime.Now;
                    OnPropertyChanged(nameof(LastUpdatedText));
                    
                    Debug.WriteLine("Chart refresh completed");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing chart: {ex.Message}");
                await DisplayAlert("Refresh Failed", "Failed to refresh chart data. Please try again later.", "OK");
            }
        }
        
        private void OnTimeRangeSelected(object sender, EventArgs e)
        {
            if (sender is Border border && border.BindingContext is string timeRangeText)
            {
                this.Dispatcher.Dispatch(async () => 
                {
                    var originalBackground = border.BackgroundColor;
                    var originalTextColor = (border.Content as Label)?.TextColor;
                    
                    border.BackgroundColor = Colors.LightGray;
                    if (border.Content is Label label)
                    {
                        label.TextColor = Colors.Black;
                    }
                    
                    await Task.Delay(150);
                    
                    SelectedTimeRange = timeRangeText switch
                    {
                        "1w" => TimeRange.Week,
                        "1m" => TimeRange.Month,
                        "1y" => TimeRange.Year,
                        _ => TimeRange.Week
                    };
                    
                    border.BackgroundColor = originalBackground;
                    if (border.Content is Label label2)
                    {
                        label2.TextColor = originalTextColor;
                    }
                });
            }
        }
        
        private async void OnBaseCurrencyPickerClicked(object sender, EventArgs e)
        {
            string result = await DisplayActionSheet("Select Base Currency", "Cancel", null, AvailableCurrencies.ToArray());
            if (!string.IsNullOrEmpty(result) && result != "Cancel")
            {
                BaseCurrency = result;
            }
        }
        
        private async void OnTargetCurrencyPickerClicked(object sender, EventArgs e)
        {
            string result = await DisplayActionSheet("Select Target Currency", "Cancel", null, AvailableCurrencies.ToArray());
            if (!string.IsNullOrEmpty(result) && result != "Cancel")
            {
                TargetCurrency = result;
            }
        }
        
        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Navigation.PopToRootAsync();
        }
        
        private async void OnWalletClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new WalletPage());
        }
        
        private async void OnTrendsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ExchangeRateTrendsPage());
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
        
        private void UpdateTimeRangeUI()
        {
            OnPropertyChanged(nameof(WeekBackground));
            OnPropertyChanged(nameof(WeekTextColor));
            OnPropertyChanged(nameof(MonthBackground));
            OnPropertyChanged(nameof(MonthTextColor));
            OnPropertyChanged(nameof(YearBackground));
            OnPropertyChanged(nameof(YearTextColor));
            
            this.Dispatcher.Dispatch(async () => 
            {
                try
                {
                    var weekButton = this.FindByName<Border>("weekButton");
                    var monthButton = this.FindByName<Border>("monthButton");
                    var yearButton = this.FindByName<Border>("yearButton");
                    
                    switch (SelectedTimeRange)
                    {
                        case TimeRange.Week:
                            if (weekButton != null)
                            {
                                await AnimateButtonSelectionAsync(weekButton);
                            }
                            break;
                        case TimeRange.Month:
                            if (monthButton != null)
                            {
                                await AnimateButtonSelectionAsync(monthButton);
                            }
                            break;
                        case TimeRange.Year:
                            if (yearButton != null)
                            {
                                await AnimateButtonSelectionAsync(yearButton);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating time range UI: {ex.Message}");
                }
            });
        }
        
        private async Task AnimateButtonSelectionAsync(Border button)
        {
            try
            {
                var originalScale = button.Scale;
                
                await button.ScaleTo(1.1, 150, Easing.SpringOut);
                
                await button.ScaleTo(originalScale, 150, Easing.SpringIn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error animating button: {ex.Message}");
            }
        }
        
        private void UpdateCurrencyFlags()
        {
            this.Dispatcher.Dispatch(() => {
                try
                {
                    var baseFlag = FindByName("baseCurrencyFlag") as Image;
                    if (baseFlag != null)
                    {
                        baseFlag.Source = $"flag_{BaseCurrency.ToLower()}.png";
                    }
                    
                    var targetFlag = FindByName("targetCurrencyFlag") as Image;
                    if (targetFlag != null)
                    {
                        targetFlag.Source = $"flag_{TargetCurrency.ToLower()}.png";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating currency flags: {ex.Message}");
                }
            });
        }
        
        private void OnDataPointTapped(object sender, EventArgs e)
        {
        }
        

    }
    
    public class ExchangeRateDataPoint : INotifyPropertyChanged
    {
        private DateTime _date;
        private decimal _value;
        private decimal _change;
        private string _changeText;
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        public DateTime Date 
        { 
            get => _date; 
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }
        
        public DateTime Time => Date;
        
        public decimal Value 
        { 
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }
        
        public decimal Change
        {
            get => _change;
            set
            {
                _change = value;
                ChangeText = value >= 0 ? $"+{value:F4}" : $"{value:F4}";
                OnPropertyChanged();
            }
        }
        
        public string ChangeText
        {
            get => _changeText;
            set
            {
                _changeText = value;
                OnPropertyChanged();
            }
        }
        
        public string ChangeColor => Change >= 0 ? "#29A745" : "#DC3545";
        
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
} 
