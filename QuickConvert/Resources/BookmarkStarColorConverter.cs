using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace QuickConvert.Resources
{
    public class BookmarkStarColorConverter : IMultiValueConverter, IValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("BookmarkStarColorConverter.Convert (multi) called");
            
            if (values.Length < 2)
                return Colors.Gray;
                
            var currencyCode = values[0] as string;
            var bookmarked = values[1] as ObservableCollection<string>;
            
            if (currencyCode == null)
            {
                Debug.WriteLine("Currency code is null");
                return Colors.Gray;
            }
            
            if (bookmarked == null)
            {
                Debug.WriteLine("Bookmarked collection is null");
                return Colors.Gray;
            }
            
            Debug.WriteLine($"Checking if {currencyCode} is in bookmarked collection with {bookmarked.Count} items");
            
            if (bookmarked.Contains(currencyCode))
            {
                Debug.WriteLine($"{currencyCode} is bookmarked - returning Gold");
                return Colors.Gold;
            }
            
            Debug.WriteLine($"{currencyCode} is not bookmarked - returning Gray");
            return Colors.Gray;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("BookmarkStarColorConverter.Convert (single) called");
            
            var currencyCode = value as string;
            
            if (currencyCode == null)
            {
                Debug.WriteLine("Currency code is null in single value converter");
                return Colors.Gray;
            }
            
            var bookmarked = BookmarkStore.BookmarkedCurrencies;
            
            if (bookmarked == null)
            {
                Debug.WriteLine("BookmarkStore.BookmarkedCurrencies is null");
                return Colors.Gray;
            }
            
            Debug.WriteLine($"Checking if {currencyCode} is in bookmarked collection with {bookmarked.Count} items");
            
            if (bookmarked.Contains(currencyCode))
            {
                Debug.WriteLine($"{currencyCode} is bookmarked - returning Gold");
                return Colors.Gold;
            }
            
            Debug.WriteLine($"{currencyCode} is not bookmarked - returning Gray");
            return Colors.Gray;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
