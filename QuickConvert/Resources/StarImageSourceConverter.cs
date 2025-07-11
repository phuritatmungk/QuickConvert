using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace QuickConvert.Resources
{
    public class StarImageSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var currencyCode = value as string;
            
            if (MainPage.Instance != null && 
                currencyCode != null && 
                MainPage.Instance.DbBookmarkedCurrencies.Contains(currencyCode))
            {
                return "star_filled.png"; 
            }
            
            if (currencyCode != null && BookmarkStore.BookmarkedCurrencies.Contains(currencyCode))
                return "star_filled.png";
                
            return "star_outline.png";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
