namespace AnythingToMp3Converter.UI.Extensions
{
    using System.Windows;

    public static class FrameworkElementExtensions
    {
        public static bool IsNotNull(this FrameworkElement element) => element != null;
        public static void Show(this FrameworkElement element)
        {
            if (element.IsNotNull())
            {
                element.Enable();
                element.Visibility = Visibility.Visible;
            }
        }
        public static void Hide(this FrameworkElement element)
        {
            if (element.IsNotNull())
            {
                element.Disable();
                element.Visibility = Visibility.Hidden;
            }
        }
        public static void Collapse(this FrameworkElement element)
        {
            if (element.IsNotNull())
            {
                element.Disable();
                element.Visibility = Visibility.Collapsed;
            }
        }
        public static void Enable(this FrameworkElement element)
        {
            if (element.IsNotNull()) element.IsEnabled = true;
        }
        public static void Disable(this FrameworkElement element)
        {
            if (element.IsNotNull()) element.IsEnabled = false;
        }
    }
}
