namespace AnythingToMp3Converter.UI.Converters
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;

    public class FileNameLimitConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fileName)
            {
                fileName = string.Concat(@"...\", fileName);
                return fileName.Length > 63 ? string.Concat(fileName.Substring(0, 60), "...", $".{fileName.Split(".").Last()}") : fileName;
            }

            return value;
        }
    }
}
