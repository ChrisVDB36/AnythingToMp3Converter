namespace AnythingToMp3Converter.UI.Extensions
{
    public static class CustomExtensions
    {
        public static string RemoveValue(this string value, string oldValue, string newValue = null)
        {
            return string.IsNullOrWhiteSpace(oldValue) ? value : value.Replace(oldValue, !string.IsNullOrWhiteSpace(newValue) ? newValue : string.Empty);
        }
    }
}
