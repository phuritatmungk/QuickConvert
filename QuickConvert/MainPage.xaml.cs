using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;
using System.Windows.Input;
using System.Globalization;
using Microsoft.Maui.Controls;
using QuickConvert.Services;
using QuickConvert.Models;

namespace QuickConvert;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    private readonly HttpClient _httpClient = new();
    private readonly DatabaseService? _databaseService;
    private DateTime _selectedDate = DateTime.Today;
    private string _rateDateText = string.Empty;
    private Dictionary<string, CurrencyRate> _ratesCache = new();
    private int _bookmarkRefreshKey;
    private bool _isDarkMode = false;

    public new event PropertyChangedEventHandler? PropertyChanged;

    public static MainPage? Instance { get; private set; }

    public static string StaticSourceCurrency
    {
        set
        {
            if (Instance != null)
                Instance.SourceCurrency = value;
        }
    }

    public static string StaticTargetCurrency
    {
        set
        {
            if (Instance != null)
                Instance.TargetCurrency = value;
        }
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                RateDateText = value.ToString("yyyy-MM-dd");
                LoadRatesAsync().ContinueWith(_ =>
                {
                    CalculateTargetAmount();
                });
            }
        }
    }

    public string RateDateText
    {
        get => _rateDateText;
        set => SetProperty(ref _rateDateText, value);
    }

    public ObservableCollection<string> Currencies { get; } = new();
    
    public ObservableCollection<string> BookmarkedCurrencies => BookmarkStore.BookmarkedCurrencies;
    
    private ObservableCollection<string> _dbBookmarkedCurrencies = new();
    public ObservableCollection<string> DbBookmarkedCurrencies
    {
        get => _dbBookmarkedCurrencies;
        private set => SetProperty(ref _dbBookmarkedCurrencies, value);
    }

    private string _sourceCurrency = string.Empty;
    public string SourceCurrency
    {
        get => _sourceCurrency;
        set
        {
            if (SetProperty(ref _sourceCurrency, value))
            {
                if (value == "JPY") 
                {
                    SourceAmount = "100"; 
                }
                CalculateTargetAmount();
            }
        }
    }

    private string _targetCurrency = string.Empty;
    public string TargetCurrency
    {
        get => _targetCurrency;
        set
        {
            if (SetProperty(ref _targetCurrency, value))
                CalculateTargetAmount();
        }
    }

    private string _sourceAmount = string.Empty;
    public string SourceAmount
    {
        get => _sourceAmount;
        set
        {
            if (SetProperty(ref _sourceAmount, value))
            {
                if (decimal.TryParse(value, out var amount))
                {
                    _sourceDecimalAmount = amount;
                    CalculateTargetAmount();
                }
                else
                {
                    _sourceDecimalAmount = 0;
                }
            }
        }
    }

    private string _targetAmount = string.Empty;
    public string TargetAmount
    {
        get => _targetAmount;
        set => SetProperty(ref _targetAmount, value);
    }

    private decimal _sourceDecimalAmount;

    public ICommand SwapCurrenciesCommand { get; }
    public ICommand BookmarkCommand { get; }
    public ICommand ToggleBookmarkCommand { get; }

    private string _table1 = string.Empty;
    public string Table1
    {
        get => _table1;
        set => SetProperty(ref _table1, value);
    }

    private string _table5 = string.Empty;
    public string Table5
    {
        get => _table5;
        set => SetProperty(ref _table5, value);
    }

    private string _table10 = string.Empty;
    public string Table10
    {
        get => _table10;
        set => SetProperty(ref _table10, value);
    }

    private string _table50 = string.Empty;
    public string Table50
    {
        get => _table50;
        set => SetProperty(ref _table50, value);
    }

    private string _table100 = string.Empty;
    public string Table100
    {
        get => _table100;
        set => SetProperty(ref _table100, value);
    }

    private string _table1000 = string.Empty;
    public string Table1000
    {
        get => _table1000;
        set => SetProperty(ref _table1000, value);
    }

    public int BookmarkRefreshKey
    {
        get => _bookmarkRefreshKey;
        set { _bookmarkRefreshKey = value; OnPropertyChanged(nameof(BookmarkRefreshKey)); }
    }

    public MainPage() : this(App.DatabaseService ?? 
        IPlatformApplication.Current?.Services.GetService<DatabaseService>() ?? 
        new DatabaseService())
    {
    }

    public MainPage(DatabaseService? databaseService)
    {
        Instance = this;
        InitializeComponent();
        _databaseService = databaseService;
        
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        BindingContext = this;
        
        SwapCurrenciesCommand = new Command(SwapCurrencies);
        BookmarkCommand = new Command(async () => await GoToBookmarkPage());
        ToggleBookmarkCommand = new Command<string>(ToggleBookmark);
        
        LoadPreferences();
        
        _sourceDecimalAmount = 1;
        
        UpdateCurrencies();
        SourceAmount = "1";
        
        CleanupDuplicateBookmarks();
        
        LoadBookmarksFromDatabase();
        
        BookmarkedCurrencies.CollectionChanged += (s, e) => {
            OnPropertyChanged(nameof(BookmarkedCurrencies));
            BookmarkRefreshKey++;
        };
        
        LoadPreferences();
        _ = LoadRatesAsync();
        
        _isDarkMode = Preferences.Get("IsDarkMode", false);
        ApplyTheme(_isDarkMode);
    }
    
    private async Task LoadRatesAsync()
    {
        DateTime date = SelectedDate;
        Debug.WriteLine($"Загрузка курсов для даты: {date}");

        while (true)
        {
            var rates = await GetRatesAsync(date);
            if (rates != null)
            {
                _ratesCache = rates;
                RateDateText = $"Rate for {date:dd.MM.yyyy}";

                UpdateCurrencies();      
                CalculateTargetAmount(); 

                SavePreferences();       
                break;
            }
            date = date.AddDays(-1);
            if (date < DateTime.Today.AddYears(-1))
            {
                Debug.WriteLine("Data for the selected date is not available.");
                RateDateText = "Rates are not available for the selected date";
                break;
            }
        }
    }

    private async Task<Dictionary<string, CurrencyRate>> GetRatesAsync(DateTime date)
    {
        string url = "https://open.er-api.com/v6/latest/USD";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);
            if (response.TryGetProperty("rates", out var rates))
            {
                var ratesDict = new Dictionary<string, CurrencyRate>();
                foreach (var rate in rates.EnumerateObject())
                {
                    ratesDict[rate.Name] = new CurrencyRate
                    {
                        CharCode = rate.Name,
                        Name = GetCurrencyName(rate.Name),
                        Value = decimal.Parse(rate.Value.ToString()),
                        Nominal = 1
                    };
                }
                return ratesDict;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching exchange rates: {ex.Message}");
        }
        return null;
    }

    private string GetCurrencyName(string code) => code switch
    {
        "USD" => "US Dollar",
        "THB" => "Thai Baht",
        "EUR" => "Euro",
        "GBP" => "British Pound",
        "JPY" => "Japanese Yen",
        "CNY" => "Chinese Yuan",
        _ => code
    };

    private void UpdateCurrencies()
    {
        var previousSourceCurrency = SourceCurrency; 
        var previousTargetCurrency = TargetCurrency;

        Currencies.Clear();
        foreach (var rate in _ratesCache.Values)
        {
            Currencies.Add(rate.CharCode);
        }

        if (Currencies.Contains(previousSourceCurrency))
            SourceCurrency = previousSourceCurrency;
        if (Currencies.Contains(previousTargetCurrency))
            TargetCurrency = previousTargetCurrency;

        if (string.IsNullOrEmpty(SourceCurrency) || !Currencies.Contains(SourceCurrency))
            SourceCurrency = "USD";
        if (string.IsNullOrEmpty(TargetCurrency) || !Currencies.Contains(TargetCurrency))
            TargetCurrency = "THB";
    }

    public void CalculateTargetAmount()
    {
        if (string.IsNullOrEmpty(SourceCurrency) || string.IsNullOrEmpty(TargetCurrency) ||
            !_ratesCache.ContainsKey(SourceCurrency) || !_ratesCache.ContainsKey(TargetCurrency))
        {
            return;
        }

        var sourceRate = _ratesCache[SourceCurrency];
        var targetRate = _ratesCache[TargetCurrency];
        decimal result = _sourceDecimalAmount * (targetRate.Value / sourceRate.Value);

        TargetAmount = result.ToString("F2");
        UpdateConversionTable();
    }

    private void UpdateConversionTable()
    {
        if (string.IsNullOrEmpty(SourceCurrency) || string.IsNullOrEmpty(TargetCurrency) ||
            !_ratesCache.ContainsKey(SourceCurrency) || !_ratesCache.ContainsKey(TargetCurrency))
        {
            return;
        }

        var sourceRate = _ratesCache[SourceCurrency];
        var targetRate = _ratesCache[TargetCurrency];

        decimal[] amounts = { 1, 5, 10, 50, 100, 1000 };
        string[] tableProperties = { nameof(Table1), nameof(Table5), nameof(Table10), nameof(Table50), nameof(Table100), nameof(Table1000) };

        for (int i = 0; i < amounts.Length; i++)
        {
            decimal result = amounts[i] * (targetRate.Value / sourceRate.Value);
            OnPropertyChanged(tableProperties[i]);
            switch (i)
            {
                case 0: Table1 = result.ToString("F2"); break;
                case 1: Table5 = result.ToString("F2"); break;
                case 2: Table10 = result.ToString("F2"); break;
                case 3: Table50 = result.ToString("F2"); break;
                case 4: Table100 = result.ToString("F2"); break;
                case 5: Table1000 = result.ToString("F2"); break;
            }
        }
    }

    private void LoadPreferences()
    {
        SelectedDate = Preferences.Get("SelectedDate", DateTime.Today);
        SourceAmount = Preferences.Get("SourceAmount", "1");
    }

    private void SavePreferences()
    {
        Preferences.Set("SelectedDate", SelectedDate);
        Preferences.Set("SourceCurrency", SourceCurrency);
        Preferences.Set("TargetCurrency", TargetCurrency);
        Preferences.Set("SourceAmount", SourceAmount);
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SwapCurrencies()
    {
        if (string.IsNullOrEmpty(SourceCurrency) || string.IsNullOrEmpty(TargetCurrency))
            return;

        var temp = SourceCurrency;
        SourceCurrency = TargetCurrency;
        TargetCurrency = temp;
    }

    private async Task GoToBookmarkPage()
    {
        await Shell.Current.GoToAsync("///BookmarkPage");
    }

    private async void ToggleBookmark(string currencyCode)
    {
        Debug.WriteLine($"ToggleBookmark called for currency: {currencyCode}");
        
        OnPropertyChanged(nameof(BookmarkedCurrencies));
        
        if (string.IsNullOrEmpty(currencyCode))
        {
            Debug.WriteLine("ERROR: currencyCode is null or empty");
            return;
        }
        
        try
        {
            bool isBookmarked = BookmarkedCurrencies.Contains(currencyCode);
            Debug.WriteLine($"Currency {currencyCode} is currently bookmarked: {isBookmarked}");
            
            if (isBookmarked)
            {
                BookmarkedCurrencies.Remove(currencyCode);
                Debug.WriteLine($"Removed {currencyCode} from BookmarkedCurrencies collection");
                
                if (DbBookmarkedCurrencies.Contains(currencyCode))
                {
                    DbBookmarkedCurrencies.Remove(currencyCode);
                    Debug.WriteLine($"Removed {currencyCode} from DbBookmarkedCurrencies collection");
                }
                
                var bookmark = await _databaseService.GetBookmarkByCurrencyCodeAsync(currencyCode);
                if (bookmark != null)
                {
                    var result = await _databaseService.DeleteBookmarkAsync(bookmark);
                    Debug.WriteLine($"Deleted bookmark from database, result: {result}");
                }
                else
                {
                    Debug.WriteLine($"No database record found for {currencyCode} to delete");
                }
            }
            else
            {
                BookmarkedCurrencies.Add(currencyCode);
                Debug.WriteLine($"Added {currencyCode} to BookmarkedCurrencies collection");
                
                if (!DbBookmarkedCurrencies.Contains(currencyCode))
                {
                    DbBookmarkedCurrencies.Add(currencyCode);
                    Debug.WriteLine($"Added {currencyCode} to DbBookmarkedCurrencies collection");
                }
                
                var newBookmark = new BookmarkDbItem { CurrencyCode = currencyCode };
                var result = await _databaseService.SaveBookmarkAsync(newBookmark);
                Debug.WriteLine($"Saved bookmark to database, result ID: {result}");
            }
            
            OnPropertyChanged(nameof(BookmarkedCurrencies));
            OnPropertyChanged(nameof(DbBookmarkedCurrencies));
            BookmarkRefreshKey++;
            Debug.WriteLine($"Updated BookmarkRefreshKey to: {BookmarkRefreshKey}");
            
            Debug.WriteLine($"Current bookmarks state:");
            Debug.WriteLine($"  BookmarkedCurrencies count: {BookmarkedCurrencies.Count}");
            Debug.WriteLine($"  DbBookmarkedCurrencies count: {DbBookmarkedCurrencies.Count}");
            
            foreach (var code in BookmarkedCurrencies)
            {
                Debug.WriteLine($"  BookmarkedCurrencies contains: {code}");
            }
            
            foreach (var code in DbBookmarkedCurrencies)
            {
                Debug.WriteLine($"  DbBookmarkedCurrencies contains: {code}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR toggling bookmark: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async void OnSourceCurrencyButtonClicked(object sender, EventArgs e)
    {
        var picker = new CurrencyPickerPage(Currencies);
        picker.CurrencySelected += (selected) => SourceCurrency = selected;
        await Navigation.PushModalAsync(picker);
    }

    private async void OnTargetCurrencyButtonClicked(object sender, EventArgs e)
    {
        var picker = new CurrencyPickerPage(Currencies);
        picker.CurrencySelected += (selected) => TargetCurrency = selected;
        await Navigation.PushModalAsync(picker);
    }

    private void OnDarkModeToggleClicked(object sender, EventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        ApplyTheme(_isDarkMode);
        Preferences.Set("IsDarkMode", _isDarkMode);
    }

    private void ApplyTheme(bool isDarkMode)
    {
        _isDarkMode = isDarkMode;
        Application.Current.UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
        DarkModeToggle.Text = isDarkMode ? "☀️" : "🌙";
    }

    private async void LoadBookmarksFromDatabase()
    {
        try
        {
            var bookmarks = await _databaseService.GetBookmarksAsync();
            
            DbBookmarkedCurrencies.Clear();
            
            foreach (var bookmark in bookmarks)
            {
                if (!DbBookmarkedCurrencies.Contains(bookmark.CurrencyCode))
                {
                    DbBookmarkedCurrencies.Add(bookmark.CurrencyCode);
                }
                
                if (!BookmarkedCurrencies.Contains(bookmark.CurrencyCode))
                {
                    BookmarkedCurrencies.Add(bookmark.CurrencyCode);
                }
            }
            
            foreach (var code in BookmarkedCurrencies)
            {
                if (!DbBookmarkedCurrencies.Contains(code))
                {
                    var newBookmark = new BookmarkDbItem { CurrencyCode = code };
                    await _databaseService.SaveBookmarkAsync(newBookmark);
                    DbBookmarkedCurrencies.Add(code);
                }
            }
            
            Debug.WriteLine($"Loaded {DbBookmarkedCurrencies.Count} bookmarks from database");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading bookmarks from database: {ex.Message}");
        }
    }

    private async void CleanupDuplicateBookmarks()
    {
        return;
        
        /*
        try
        {
            var bookmarks = await _databaseService.GetBookmarksAsync();
            var processedCurrencies = new HashSet<string>();
            var duplicatesToRemove = new List<BookmarkDbItem>();
            
            foreach (var bookmark in bookmarks)
            {
                if (processedCurrencies.Contains(bookmark.CurrencyCode))
                {
                    // This is a duplicate, mark for removal
                    duplicatesToRemove.Add(bookmark);
                }
                else
                {
                    processedCurrencies.Add(bookmark.CurrencyCode);
                }
            }
            
            // Remove all duplicates
            foreach (var duplicate in duplicatesToRemove)
            {
                await _databaseService.DeleteBookmarkAsync(duplicate);
                Debug.WriteLine($"Deleted duplicate bookmark for {duplicate.CurrencyCode}");
            }
            
            if (duplicatesToRemove.Count > 0)
            {
                Debug.WriteLine($"Removed {duplicatesToRemove.Count} duplicate bookmarks");
                LoadBookmarksFromDatabase();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error cleaning up bookmarks: {ex.Message}");
        }
        */
    }

    private void OnBookmarkTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string currencyCode)
        {
            Debug.WriteLine($"Bookmark star tapped for {currencyCode}");
            ToggleBookmark(currencyCode);
        }
        else
        {
            Debug.WriteLine("Bookmark tapped but no currency code provided");
        }
    }

    private async void OnBookmarkButtonClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("Bookmark button clicked, navigating to BookmarkPage");
        
        try
        {
            await Shell.Current.GoToAsync("///BookmarkPage");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error navigating to BookmarkPage: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void OnSwapButtonClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("Swap button clicked");
        SwapCurrencies();
    }
}

public class CurrencyRate
{
    public required string CharCode { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public decimal Nominal { get; set; }

    public decimal GetRate(decimal amount)
    {
        return (amount * Value) / Nominal;
    }
}