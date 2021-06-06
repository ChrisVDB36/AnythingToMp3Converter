namespace AnythingToMp3Converter.UI.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class FileNameLimitConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fileName)
            {
                fileName = string.Concat(@"...\", fileName);
                return fileName.Length > 61 ? string.Concat(fileName.Substring(0, 58), "...") : fileName;
            }

            return value;
        }
    }
}
