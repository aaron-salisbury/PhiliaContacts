using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PhiliaContacts.App.Base.Converters
{
    public class DateTimeToDateTimeOffestConverter : DependencyObject, IValueConverter
    {
        public bool PreserveDateOnConvertBack
        {
            get { return (bool)GetValue(PreserveDateOnConvertBackProperty); }
            set { SetValue(PreserveDateOnConvertBackProperty, value); }
        }

        public static readonly DependencyProperty PreserveDateOnConvertBackProperty =
            DependencyProperty.Register(
            nameof(PreserveDateOnConvertBack),
            typeof(bool),
            typeof(DateTimeToDateTimeOffestConverter),
            new PropertyMetadata(true));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime? thisDate = (DateTime?)value;

            if (thisDate != null)
            {
                return new DateTimeOffset(thisDate.Value);
            }
            else
            {
                return new DateTimeOffset(DateTime.Now.Date);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            //TODO: Return null if set date is false
            //      https://stackoverflow.com/questions/377841/what-should-the-converter-parameter-be-for-this-binding
            if (PreserveDateOnConvertBack)
            {
                DateTimeOffset thisDateTimeOffset = (DateTimeOffset)value;

                return thisDateTimeOffset.DateTime;
            }

            return null;
        }
    }
}
