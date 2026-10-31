using Avalonia.Data.Converters;
using PhiliaContacts.Business.Modules.Contacts;
using System;
using System.Globalization;

namespace PhiliaContacts.Presentation.Desktop.Base.Converters;

/// <summary>
/// Converts a contact's stored birthday text to and from the <see cref="DateTimeOffset"/> used by <c>DatePicker</c>.
/// Only the calendar date is used: the picker's time and offset are discarded so the date never shifts, and the value
/// written back is always <see cref="ContactBirthday.FORMAT"/> regardless of the current culture.
/// </summary>
public class BirthdayToDateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text && ContactBirthday.TryParse(text, out DateOnly date)
            ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DateTimeOffset offset => ContactBirthday.Format(DateOnly.FromDateTime(offset.DateTime)),
            DateTime dateTime => ContactBirthday.Format(DateOnly.FromDateTime(dateTime)),
            _ => null
        };
    }
}
