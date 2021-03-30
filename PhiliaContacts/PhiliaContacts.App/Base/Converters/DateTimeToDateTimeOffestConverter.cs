using System;
using Windows.UI.Xaml.Data;

namespace PhiliaContacts.App.Base.Converters
{
    public class DateTimeToDateTimeOffestConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime? thisDate = (DateTime?)value;

            if (thisDate != null)
            {
                return new DateTimeOffset(thisDate.Value);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                DateTimeOffset thisDateTimeOffset = (DateTimeOffset)value;

                return thisDateTimeOffset.DateTime;
            }

            return null;
        }
    }
}
