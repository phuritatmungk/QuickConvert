using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System.Diagnostics;
using QuickConvert.Services;
using QuickConvert.Models;
using System.Collections.Generic;
using System.Linq;

namespace QuickConvert;

public partial class BookmarkPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public BookmarkPage() : this(App.DatabaseService ?? 
        IPlatformApplication.Current.Services.GetService<DatabaseService>() ?? 
        new DatabaseService())
    {
    }

    public BookmarkPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        BindingContext = new BookmarkPageViewModel(Navigation, _databaseService);
        
        Debug.WriteLine("BookmarkPage initialized");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        Debug.WriteLine("BookmarkPage.OnAppearing called");
        
        try
        {
            var bookmarks = await _databaseService.GetBookmarksAsync();
            Debug.WriteLine($"Found {bookmarks.Count} bookmarks in database:");
            foreach (var bookmark in bookmarks)
            {
                Debug.WriteLine($"  ID: {bookmark.Id}, Currency: {bookmark.CurrencyCode}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking bookmarks: {ex.Message}");
        }
        
        if (BindingContext is BookmarkPageViewModel vm)
        {
            vm.RefreshBookmarks();
        }
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("Back button clicked");
        await NavigateBackToTab();
    }
    
    private async void OnCurrencyItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string currencyCode)
        {
            Debug.WriteLine($"Currency item tapped: {currencyCode}");
            try
            {
                string action = await Application.Current.MainPage.DisplayActionSheet(
                    $"Set {currencyCode} as:", "Cancel", null, "Source Currency", "Target Currency");
                
                if (action == "Source Currency")
                {
                    MainPage.StaticSourceCurrency = currencyCode;
                    await NavigateBackToTab();
                }
                else if (action == "Target Currency")
                {
                    MainPage.StaticTargetCurrency = currencyCode;
                    await NavigateBackToTab();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling currency item tap: {ex.Message}");
            }
        }
    }
    
    public async Task NavigateBackToTab()
    {
        int tabIndex = AppShell.LastTabIndex;
        Debug.WriteLine($"Navigating back to tab index: {tabIndex}");
        
        try
        {
            string route = tabIndex switch
            {
                0 => "//MainPage",
                1 => "//ExchangeRateTrendsPage",
                2 => "//ChartPage",
                3 => "//WalletPage",
                _ => "//MainPage"
            };
            
            Debug.WriteLine($"Navigating to route: {route}");
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in NavigateBackToTab: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            try
            {
                await Shell.Current.GoToAsync("//MainPage");
            }
            catch (Exception fallbackEx)
            {
                Debug.WriteLine($"Fallback navigation failed: {fallbackEx.Message}");
            }
        }
    }
    
    private async void OnRemoveBookmarkClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string currencyCode)
        {
            Debug.WriteLine($"Remove bookmark clicked for {currencyCode}");
            try
            {
                if (BookmarkStore.BookmarkedCurrencies.Contains(currencyCode))
                {
                    BookmarkStore.BookmarkedCurrencies.Remove(currencyCode);
                    Debug.WriteLine($"Removed {currencyCode} from BookmarkStore.BookmarkedCurrencies");
                }
                
                var bookmark = await _databaseService.GetBookmarkByCurrencyCodeAsync(currencyCode);
                if (bookmark != null)
                {
                    var result = await _databaseService.DeleteBookmarkAsync(bookmark);
                    Debug.WriteLine($"Deleted bookmark from database, result: {result}");
                }
                
                if (BindingContext is BookmarkPageViewModel vm)
                {
                    vm.RefreshBookmarks();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing bookmark: {ex.Message}");
            }
        }
    }
}

public class CurrencyBookmark
{
    public int Id { get; set; }
    public string Flag { get; set; }
    public string CurrencyCode { get; set; }
    public string CurrencyName { get; set; }
    public bool IsFavorite { get; set; } = true;
}

public class BookmarkPageViewModel : BindableObject
{
    public ObservableCollection<CurrencyBookmark> Bookmarks { get; set; }
    public ICommand GoBackCommand { get; }
    public ICommand RemoveBookmarkCommand { get; }
    public ICommand SelectCurrencyCommand { get; }
    private INavigation _navigation;
    private DatabaseService _databaseService;

    private readonly Dictionary<string, string> CurrencyNames = new()
    {
        { "USD", "US Dollar" },
        { "EUR", "Euro" },
        { "GBP", "British Pound" },
        { "JPY", "Japanese Yen" },
        { "AUD", "Australian Dollar" },
        { "CAD", "Canadian Dollar" },
        { "CHF", "Swiss Franc" },
        { "CNY", "Chinese Yuan" },
        { "HKD", "Hong Kong Dollar" },
        { "NZD", "New Zealand Dollar" },
        { "SEK", "Swedish Krona" },
        { "KRW", "South Korean Won" },
        { "SGD", "Singapore Dollar" },
        { "NOK", "Norwegian Krone" },
        { "MXN", "Mexican Peso" },
        { "INR", "Indian Rupee" },
        { "RUB", "Russian Ruble" },
        { "ZAR", "South African Rand" },
        { "TRY", "Turkish Lira" },
        { "BRL", "Brazilian Real" },
        { "THB", "Thai Baht" },
        { "IDR", "Indonesian Rupiah" },
        { "MYR", "Malaysian Ringgit" },
        { "PHP", "Philippine Peso" },
        { "DKK", "Danish Krone" },
        { "PLN", "Polish Zloty" },
        { "CZK", "Czech Koruna" },
        { "HUF", "Hungarian Forint" },
        { "ILS", "Israeli Shekel" },
        { "RON", "Romanian Leu" },
        { "BGN", "Bulgarian Lev" },
        { "HRK", "Croatian Kuna" },
        { "ISK", "Icelandic Króna" },
        { "VND", "Vietnamese Dong" },
        { "EGP", "Egyptian Pound" },
        { "SAR", "Saudi Riyal" },
        { "AED", "UAE Dirham" },
        { "QAR", "Qatari Riyal" },
        { "KWD", "Kuwaiti Dinar" },
        { "BHD", "Bahraini Dinar" },
        { "OMR", "Omani Rial" },
        { "JOD", "Jordanian Dinar" },
        { "LBP", "Lebanese Pound" },
        { "PKR", "Pakistani Rupee" },
        { "BDT", "Bangladeshi Taka" },
        { "LKR", "Sri Lankan Rupee" },
        { "NPR", "Nepalese Rupee" },
        { "ARS", "Argentine Peso" },
        { "CLP", "Chilean Peso" },
        { "COP", "Colombian Peso" },
        { "PEN", "Peruvian Sol" },
        { "UYU", "Uruguayan Peso" },
        { "UAH", "Ukrainian Hryvnia" },
        { "NGN", "Nigerian Naira" },
        { "GHS", "Ghanaian Cedi" },
        { "KES", "Kenyan Shilling" },
        { "MAD", "Moroccan Dirham" },
        { "TWD", "Taiwan Dollar" },
        { "PYG", "Paraguayan Guaraní" },
        { "IQD", "Iraqi Dinar" },
        { "DZD", "Algerian Dinar" },
        { "TND", "Tunisian Dinar" },
        { "AFN", "Afghan Afghani" },
        { "BOB", "Bolivian Boliviano" },
        { "CRC", "Costa Rican Colón" },
        { "DOP", "Dominican Peso" },
        { "XAF", "Central African CFA" },
        { "XOF", "West African CFA" },
        { "XCD", "East Caribbean Dollar" }
    };

    public BookmarkPageViewModel(INavigation navigation, DatabaseService databaseService)
    {
        _navigation = navigation;
        _databaseService = databaseService;
        
        GoBackCommand = new Command(async () => {
            int tabIndex = AppShell.LastTabIndex;
            await NavigateToTab(tabIndex);
        });
        
        RemoveBookmarkCommand = new Command<string>(RemoveBookmark);
        SelectCurrencyCommand = new Command<string>(async (currencyCode) => await SelectCurrency(currencyCode));
        Bookmarks = new ObservableCollection<CurrencyBookmark>();
        
        RefreshBookmarks();
    }

    private async Task NavigateToTab(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0:
                await Shell.Current.GoToAsync("///MainPage");
                break;
            case 1:
                await Shell.Current.GoToAsync("///ExchangeRateTrendsPage");
                break;
            case 2:
                await Shell.Current.GoToAsync("///ChartPage");
                break;
            case 3:
                await Shell.Current.GoToAsync("///WalletPage");
                break;
            default:
                await Shell.Current.GoToAsync("///MainPage");
                break;
        }
    }

    public async void RefreshBookmarks()
    {
        try
        {
            Bookmarks.Clear();
            
            var bookmarkDbItems = await _databaseService.GetBookmarksAsync();
            
            foreach (var dbItem in bookmarkDbItems)
            {
                var currencyName = CurrencyNames.TryGetValue(dbItem.CurrencyCode, out var name) 
                    ? name : dbItem.CurrencyCode;
                    
                Bookmarks.Add(new CurrencyBookmark
                {
                    Id = dbItem.Id,
                    Flag = GetFlagForCurrency(dbItem.CurrencyCode),
                    CurrencyCode = dbItem.CurrencyCode,
                    CurrencyName = currencyName,
                    IsFavorite = true
                });
            }
            
            foreach (var code in BookmarkStore.BookmarkedCurrencies)
            {
                if (Bookmarks.Any(b => b.CurrencyCode == code))
                    continue;
                    
                var newBookmark = new BookmarkDbItem { CurrencyCode = code };
                await _databaseService.SaveBookmarkAsync(newBookmark);
                
                var currencyName = CurrencyNames.TryGetValue(code, out var name) 
                    ? name : code;
                
                Bookmarks.Add(new CurrencyBookmark
                {
                    Id = newBookmark.Id,
                    Flag = GetFlagForCurrency(code),
                    CurrencyCode = code,
                    CurrencyName = currencyName,
                    IsFavorite = true
                });
            }
            
            Debug.WriteLine($"Refreshed bookmarks: {Bookmarks.Count} currencies");
            OnPropertyChanged(nameof(Bookmarks));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing bookmarks: {ex.Message}");
        }
    }

    private string GetFlagForCurrency(string code)
    {
        return code switch
        {
            "USD" => "🇺🇸",
            "EUR" => "🇪🇺",
            "GBP" => "🇬🇧",
            "JPY" => "🇯🇵",
            "THB" => "🇹🇭",
            "KRW" => "🇰🇷",
            "CNY" => "🇨🇳",
            "AUD" => "🇦🇺",
            "CAD" => "🇨🇦",
            "CHF" => "🇨🇭",
            "SGD" => "🇸🇬",
            "INR" => "🇮🇳",
            "RUB" => "🇷🇺",
            "BRL" => "🇧🇷",
            "ZAR" => "🇿🇦",
            "HKD" => "🇭🇰",
            "NZD" => "🇳🇿",
            "SEK" => "🇸🇪",
            "NOK" => "🇳🇴",
            "DKK" => "🇩🇰",
            "PLN" => "🇵🇱",
            "CZK" => "🇨🇿",
            "HUF" => "🇭🇺",
            "TRY" => "🇹🇷",
            "ILS" => "🇮🇱",
            "MXN" => "🇲🇽",
            "MYR" => "🇲🇾",
            "IDR" => "🇮🇩",
            "PHP" => "🇵🇭",
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
            "RON" => "🇷🇴",
            "BGN" => "🇧🇬",
            "HRK" => "🇭🇷",
            "ISK" => "🇮🇸",
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
            "XAF" => "🇨🇫",
            "XOF" => "🇸🇳",
            "XCD" => "🇦🇬",
            _ => "🏳️"
        };
    }

    private async void RemoveBookmark(string currencyCode)
    {
        Debug.WriteLine($"RemoveBookmark called for {currencyCode}");
        
        try
        {
            if (BookmarkStore.BookmarkedCurrencies.Contains(currencyCode))
            {
                BookmarkStore.BookmarkedCurrencies.Remove(currencyCode);
                Debug.WriteLine($"Removed {currencyCode} from BookmarkStore.BookmarkedCurrencies");
            }
            else
            {
                Debug.WriteLine($"{currencyCode} not found in BookmarkStore.BookmarkedCurrencies");
            }
            
            var bookmark = await _databaseService.GetBookmarkByCurrencyCodeAsync(currencyCode);
            if (bookmark != null)
            {
                Debug.WriteLine($"Found bookmark in database with ID: {bookmark.Id}");
                var result = await _databaseService.DeleteBookmarkAsync(bookmark);
                Debug.WriteLine($"Database delete result: {result}");
            }
            else
            {
                Debug.WriteLine($"No bookmark found in database for {currencyCode}");
            }
            
            var bookmarkToRemove = Bookmarks.FirstOrDefault(b => b.CurrencyCode == currencyCode);
            if (bookmarkToRemove != null)
            {
                Bookmarks.Remove(bookmarkToRemove);
                Debug.WriteLine($"Removed {currencyCode} from UI bookmarks collection");
            }
            else
            {
                Debug.WriteLine($"No bookmark found in UI collection for {currencyCode}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing bookmark: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task SelectCurrency(string currencyCode)
    {
        string action = await Application.Current.MainPage.DisplayActionSheet(
            $"Set {currencyCode} as:", "Cancel", null, "Source Currency", "Target Currency");
        if (action == "Source Currency")
        {
            MainPage.StaticSourceCurrency = currencyCode;
            int tabIndex = AppShell.LastTabIndex;
            Debug.WriteLine($"After selecting source currency, navigating back to tab index: {tabIndex}");
            await NavigateToTab(tabIndex);
        }
        else if (action == "Target Currency")
        {
            MainPage.StaticTargetCurrency = currencyCode;
            int tabIndex = AppShell.LastTabIndex;
            Debug.WriteLine($"After selecting target currency, navigating back to tab index: {tabIndex}");
            await NavigateToTab(tabIndex);
        }
    }
} 
