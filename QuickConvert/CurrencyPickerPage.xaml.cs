using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Maui.ApplicationModel;

namespace QuickConvert;

public partial class CurrencyPickerPage : ContentPage
{
    public event Action<string> CurrencySelected;
    private readonly List<string> _allCurrencies;
    private ObservableCollection<string> _filteredCurrencies;
    private CancellationTokenSource _debounceTokenSource;

    public CurrencyPickerPage(IEnumerable<string> currencies)
    {
        InitializeComponent();
        _allCurrencies = currencies.ToList();
        _filteredCurrencies = new ObservableCollection<string>(_allCurrencies);
        CurrencyList.ItemsSource = _filteredCurrencies;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string selected)
        {
            CurrencySelected?.Invoke(selected);
            Navigation.PopModalAsync();
        }
    }
    
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;
            
            string searchText = e.NewTextValue?.Trim().ToUpper() ?? string.Empty;
            ClearSearchButton.IsVisible = !string.IsNullOrEmpty(searchText);
            
            await Task.Delay(150, token);
            
            await Task.Run(() => 
            {
                if (token.IsCancellationRequested) return;
                
                List<string> results;
                if (string.IsNullOrEmpty(searchText))
                {
                    results = _allCurrencies;
                }
                else
                {
                    results = _allCurrencies
                        .Where(c => c.ToUpper().Contains(searchText))
                        .ToList();
                }
                
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    if (token.IsCancellationRequested) return;
                    
                    UpdateFilteredResults(results);
                    Debug.WriteLine($"Filtered to {results.Count} currencies matching '{searchText}'");
                });
            }, token);
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error filtering currencies: {ex.Message}");
        }
    }
    
    private void UpdateFilteredResults(List<string> results)
    {
        _filteredCurrencies.Clear();
        foreach (var item in results)
        {
            _filteredCurrencies.Add(item);
        }
    }
    
    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        _filteredCurrencies.Clear();
        foreach (var item in _allCurrencies)
        {
            _filteredCurrencies.Add(item);
        }
        ClearSearchButton.IsVisible = false;
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
} 
