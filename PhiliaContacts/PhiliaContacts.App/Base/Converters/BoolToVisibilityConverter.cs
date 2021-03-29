using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PhiliaContacts.App.Base.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool thisBool = (bool)value;

            if (thisBool)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            Visibility thisVisibility = (Visibility)value;

            if (thisVisibility == Visibility.Visible)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
