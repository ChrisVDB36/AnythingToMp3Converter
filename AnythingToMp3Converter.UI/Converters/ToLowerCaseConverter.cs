namespace AnythingToMp3Converter.UI.Converters
{
    using AnythingToMp3Converter.UI.Enums;
    using System;
    using System.Globalization;
    using System.Windows.Data;

    internal class ToLowerCaseConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileStatus status) return status.ToString().ToLower();
            if (value is string text) return text.ToLower();
            return value;
        }
    }
}
