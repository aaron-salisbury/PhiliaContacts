using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PhiliaContacts.Presentation.Base.Converters
{
    public class DateTimeToDateTimeOffsetConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is DateTime thisDate)
            {
                return new DateTimeOffset(thisDate);
            }
            else
            {
                return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
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
