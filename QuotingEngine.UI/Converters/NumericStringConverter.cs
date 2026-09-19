using System;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Data;

namespace QuotingEngine.UI.Converters;

public class NumericStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string strValue)
        {
            // Remove any non-numeric characters except '.' and '-'
            var cleanString = Regex.Replace(strValue, @"[^\d.-]", "");
            
            if (decimal.TryParse(cleanString, out var result))
            {
                return result;
            }
        }
        return 0m;
    }
}
