using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Globalization;

namespace QuickConvert.Converters
{
    public class StringPathToGeometryConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string pathData && !string.IsNullOrEmpty(pathData))
            {
                try
                {
                    var converter = new PathGeometryConverter();
                    return converter.ConvertFromInvariantString(pathData);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error converting path data: {ex.Message}");
                    return new PathGeometry();
                }
            }
            return new PathGeometry();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
