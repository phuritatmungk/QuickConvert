using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text.Json;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Linq;
using QuickConvert.Services;
using QuickConvert.Models;

namespace QuickConvert
{
    public partial class WalletPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly DatabaseService _databaseService;
        public ObservableCollection<WalletItem> WalletItems { get; set; }
        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }
        private Dictionary<string, string> _currencyNames;

        public WalletPage() : this(App.DatabaseService ?? 
            IPlatformApplication.Current.Services.GetService<DatabaseService>() ?? 
            new DatabaseService())
        {
        }

        public WalletPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _databaseService = databaseService;
            WalletItems = new ObservableCollection<WalletItem>();
            DeleteCommand = new Command<WalletItem>(OnDeleteItem);
            EditCommand = new Command<WalletItem>(OnEditItem);
            _currencyNames = new Dictionary<string, string>();
            BindingContext = this;

            var addButton = this.FindByName<Label>("AddButton");
            if (addButton != null)
            {
                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += OnAddCurrencyClicked;
                addButton.GestureRecognizers.Add(tapGestureRecognizer);
            }

            LoadCurrencyNames();
            
            _ = LoadWalletItemsFromDatabaseAsync();
        }

        private async Task LoadWalletItemsFromDatabaseAsync()
        {
            try
            {
                var walletDbItems = await _databaseService.GetWalletItemsAsync();
                WalletItems.Clear();
                foreach (var dbItem in walletDbItems)
                {
                    WalletItems.Add(new WalletItem
                    {
                        Id = dbItem.Id,
                        Currency = dbItem.Currency,
                        Balance = dbItem.Balance,
                        Flag = GetFlagForCurrency(dbItem.Currency)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading wallet items: {ex.Message}");
            }
        }

        private async Task SaveWalletItemToDatabaseAsync(WalletItem item)
        {
            try
            {
                var dbItem = new WalletDbItem
                {
                    Id = item.Id,
                    Currency = item.Currency,
                    Balance = item.Balance
                };
                
                await _databaseService.SaveWalletItemAsync(dbItem);
                
                if (item.Id == 0)
                {
                    var savedItem = await _databaseService.GetWalletItemByCurrencyAsync(item.Currency);
                    if (savedItem != null)
                    {
                        item.Id = savedItem.Id;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving wallet item: {ex.Message}");
            }
        }

        private async Task DeleteWalletItemFromDatabaseAsync(WalletItem item)
        {
            try
            {
                if (item.Id != 0)
                {
                    await _databaseService.DeleteWalletItemAsync(item.Id);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting wallet item: {ex.Message}");
            }
        }

        private async void LoadCurrencyNames()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<JsonElement>("https://open.er-api.com/v6/latest/USD");
                if (response.TryGetProperty("rates", out var rates))
                {
                    foreach (var rate in rates.EnumerateObject())
                    {
                        _currencyNames[rate.Name] = GetCurrencyName(rate.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading currency names: {ex.Message}");
            }
        }

        private string GetCurrencyName(string code)
        {
            switch (code)
            {
                case "USD": return "US Dollar";
                case "EUR": return "Euro";
                case "GBP": return "British Pound";
                case "JPY": return "Japanese Yen";
                case "AUD": return "Australian Dollar";
                case "CAD": return "Canadian Dollar";
                case "CHF": return "Swiss Franc";
                case "CNY": return "Chinese Yuan";
                case "INR": return "Indian Rupee";
                case "THB": return "Thai Baht";
                case "SGD": return "Singapore Dollar";
                case "MYR": return "Malaysian Ringgit";
                case "IDR": return "Indonesian Rupiah";
                case "KRW": return "South Korean Won";
                case "NZD": return "New Zealand Dollar";
                case "HKD": return "Hong Kong Dollar";
                case "TWD": return "Taiwan Dollar";
                case "PHP": return "Philippine Peso";
                case "VND": return "Vietnamese Dong";
                case "BRL": return "Brazilian Real";
                case "RUB": return "Russian Ruble";
                case "ZAR": return "South African Rand";
                case "MXN": return "Mexican Peso";
                case "SEK": return "Swedish Krona";
                case "NOK": return "Norwegian Krone";
                case "DKK": return "Danish Krone";
                case "PLN": return "Polish Złoty";
                case "TRY": return "Turkish Lira";
                case "AED": return "UAE Dirham";
                case "SAR": return "Saudi Riyal";
                default: return code;
            }
        }

        private string GetFlagForCurrency(string currency)
        {
            return currency switch
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
                "MMK" => "🇲🇲",
                "KZT" => "🇰🇿",
                "UZS" => "🇺🇿",
                "AZN" => "🇦🇿",
                "GEL" => "🇬🇪",
                "BYN" => "🇧🇾",
                "MDL" => "🇲🇩",
                "UAH" => "🇺🇦",
                "TJS" => "🇹🇯",
                "TMT" => "🇹🇲",
                "RSD" => "🇷🇸",
                "BGN" => "🇧🇬",
                "HRK" => "🇭🇷",
                "RON" => "🇷🇴",
                "XDR" => "🌐",
                "AFN" => "🇦🇫",
                "ALL" => "🇦🇱",
                "AMD" => "🇦🇲",
                "ANG" => "🇳🇱",
                "AOA" => "🇦🇴",
                "ARS" => "🇦🇷",
                "AWG" => "🇦🇼",
                "BAM" => "🇧🇦",
                "BBD" => "🇧🇧",
                "BIF" => "🇧🇮",
                "BMD" => "🇧🇲",
                "BND" => "🇧🇳",
                "BOB" => "🇧🇴",
                "BSD" => "🇧🇸",
                "BWP" => "🇧🇼",
                "BZD" => "🇧🇿",
                "CLP" => "🇨🇱",
                "COP" => "🇨🇴",
                "CRC" => "🇨🇷",
                "CUP" => "🇨🇺",
                "CVE" => "🇨🇻",
                "DJF" => "🇩🇯",
                "DOP" => "🇩🇴",
                "DZD" => "🇩🇿",
                "ERN" => "🇪🇷",
                "ETB" => "🇪🇹",
                "FJD" => "🇫🇯",
                "FKP" => "🇫🇰",
                "FOK" => "🇫🇴",
                "GGP" => "🇬🇬",
                "GHS" => "🇬🇭",
                "GIP" => "🇬🇮",
                "GMD" => "🇬🇲",
                "GNF" => "🇬🇳",
                "GTQ" => "🇬🇹",
                "GYD" => "🇬🇾",
                "HNL" => "🇭🇳",
                "HTG" => "🇭🇹",
                "IMP" => "🇮🇲",
                "IQD" => "🇮🇶",
                "IRR" => "🇮🇷",
                "ISK" => "🇮🇸",
                "JMD" => "🇯🇲",
                "KES" => "🇰🇪",
                "KGS" => "🇰🇬",
                "KHR" => "🇰🇭",
                "KID" => "🇰🇮",
                "KMF" => "🇰🇲",
                "KYD" => "🇰🇾",
                "LAK" => "🇱🇦",
                "LSL" => "🇱🇸",
                "LYD" => "🇱🇾",
                "MAD" => "🇲🇦",
                "MGA" => "🇲🇬",
                "MKD" => "🇲🇰",
                "MNT" => "🇲🇳",
                "MOP" => "🇲🇴",
                "MRU" => "🇲🇷",
                "MUR" => "🇲🇺",
                "MVR" => "🇲🇻",
                "MWK" => "🇲🇼",
                "MZN" => "🇲🇿",
                "NAD" => "🇳🇦",
                "NGN" => "🇳🇬",
                "NIO" => "🇳🇮",
                "PAB" => "🇵🇦",
                "PEN" => "🇵🇪",
                "PGK" => "🇵🇬",
                "PYG" => "🇵🇾",
                "RWF" => "🇷🇼",
                "SBD" => "🇸🇧",
                "SCR" => "🇸🇨",
                "SDG" => "🇸🇩",
                "SHP" => "🇸🇭",
                "SLL" => "🇸🇱",
                "SOS" => "🇸🇴",
                "SRD" => "🇸🇷",
                "SSP" => "🇸🇸",
                "STN" => "🇸🇹",
                "SYP" => "🇸🇾",
                "SZL" => "🇸🇿",
                "TND" => "🇹🇳",
                "TOP" => "🇹🇴",
                "TTD" => "🇹🇹",
                "TVD" => "🇹🇻",
                "TZS" => "🇹🇿",
                "UGX" => "🇺🇬",
                "UYU" => "🇺🇾",
                "VES" => "🇻🇪",
                "VUV" => "🇻🇺",
                "WST" => "🇼🇸",
                "XAF" => "🇨🇲",
                "XCD" => "🇦🇬",
                "XOF" => "🇨🇮",
                "XPF" => "🇵🇫",
                "YER" => "🇾🇪",
                "ZMW" => "🇿🇲",
                "ZWL" => "🇿🇼",
                _ => "🏳️"
            };
        }

        private async void OnAddCurrencyClicked(object sender, EventArgs e)
        {
            var currencies = _currencyNames.Keys.OrderBy(c => c).ToList();
            var pickerPage = new CurrencyPickerPage(currencies);
            
            pickerPage.CurrencySelected += async (selectedCurrency) => {
                string balanceInput = await DisplayPromptAsync(
                    "Add Balance", 
                    $"Enter balance for {selectedCurrency}:", 
                    "Add", 
                    "Cancel", 
                    "0", 
                    keyboard: Keyboard.Numeric);
                    
                if (decimal.TryParse(balanceInput, out decimal balance))
                {
                    var existingItem = WalletItems.FirstOrDefault(i => i.Currency == selectedCurrency);
                    if (existingItem != null)
                    {
                        existingItem.Balance = balance;
                        await SaveWalletItemToDatabaseAsync(existingItem);
                    }
                    else
                    {
                        var newItem = new WalletItem 
                        { 
                            Currency = selectedCurrency, 
                            Balance = balance,
                            Flag = GetFlagForCurrency(selectedCurrency)
                        };
                        WalletItems.Add(newItem);
                        await SaveWalletItemToDatabaseAsync(newItem);
                    }
                }
            };
            
            await Navigation.PushModalAsync(pickerPage);
        }

        private async void OnDeleteItem(WalletItem item)
        {
            bool confirm = await DisplayAlert("Delete Currency", $"Are you sure you want to delete {item.Currency}?", "Yes", "No");
            if (confirm)
            {
                WalletItems.Remove(item);
                await DeleteWalletItemFromDatabaseAsync(item);
            }
        }

        private async void OnEditItem(WalletItem item)
        {
            string balanceInput = await DisplayPromptAsync("Edit Balance", $"Enter new balance for {item.Currency}:", "Save", "Cancel", item.Balance.ToString(), keyboard: Keyboard.Numeric);
            if (decimal.TryParse(balanceInput, out decimal newBalance))
            {
                item.Balance = newBalance;
                await SaveWalletItemToDatabaseAsync(item);
            }
        }
    }

    public class WalletItem : INotifyPropertyChanged
    {
        private int _id;
        private string _currency;
        private decimal _balance;
        private string _flag;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Currency
        {
            get => _currency;
            set
            {
                _currency = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public decimal Balance
        {
            get => _balance;
            set
            {
                _balance = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public string Flag
        {
            get => _flag;
            set
            {
                _flag = value;
                OnPropertyChanged();
            }
        }

        public string DisplayText => $"{Balance:N2} {Currency}";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 