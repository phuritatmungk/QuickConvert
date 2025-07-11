using System.Collections.ObjectModel;
using System.Diagnostics;

namespace QuickConvert
{
    public static class BookmarkStore
    {
        private static readonly ObservableCollection<string> _bookmarkedCurrencies = new();
        
        public static ObservableCollection<string> BookmarkedCurrencies => _bookmarkedCurrencies;
        
        static BookmarkStore()
        {
            Debug.WriteLine("BookmarkStore initialized with empty bookmarks list");
        }
        
        public static bool IsBookmarked(string currencyCode)
        {
            return _bookmarkedCurrencies.Contains(currencyCode);
        }
        
        public static bool ToggleBookmark(string currencyCode)
        {
            if (string.IsNullOrEmpty(currencyCode))
                return false;
                
            if (_bookmarkedCurrencies.Contains(currencyCode))
            {
                _bookmarkedCurrencies.Remove(currencyCode);
                Debug.WriteLine($"Removed {currencyCode} from bookmarks");
                return false;
            }
            else
            {
                _bookmarkedCurrencies.Add(currencyCode);
                Debug.WriteLine($"Added {currencyCode} to bookmarks");
                return true;
            }
        }
    }
} 
