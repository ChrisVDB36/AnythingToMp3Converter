namespace AnythingToMp3Converter.UI.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && bool.TryParse(value.ToString(), out bool isVisible)) return isVisible ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
    }
}
