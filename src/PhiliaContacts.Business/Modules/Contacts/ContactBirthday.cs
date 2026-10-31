using System;
using System.Globalization;

namespace PhiliaContacts.Business.Modules.Contacts;

/// <summary>
/// Normalizes the birthday text stored on a <see cref="Contact"/>. Birthdays are calendar dates, so any time or
/// offset in the source (for example the legacy <c>2000-01-01T13:03:02.7850172</c> shape) is discarded without
/// shifting the date. The canonical stored form is ISO 8601 <c>yyyy-MM-dd</c>.
/// </summary>
public static class ContactBirthday
{
    public const string FORMAT = "yyyy-MM-dd";

    /// <summary>
    /// Parses ISO dates (<c>yyyy-MM-dd</c>, <c>yyyyMMdd</c>) and ISO timestamps, taking the date portion as written.
    /// Yearless vCard dates (<c>--MMdd</c>) are not representable and return <c>false</c>.
    /// </summary>
    public static bool TryParse(string? text, out DateOnly date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string trimmed = text.Trim();
        int timeSeparator = trimmed.IndexOf('T');
        string datePart = timeSeparator > 0 ? trimmed[..timeSeparator] : trimmed;

        return DateOnly.TryParseExact(datePart, [FORMAT, "yyyyMMdd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    public static string Format(DateOnly date)
    {
        return date.ToString(FORMAT, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the canonical form when the text parses; otherwise returns the original text so that unrecognized
    /// values (such as yearless dates) are kept rather than lost.
    /// </summary>
    public static string? Normalize(string? text)
    {
        return TryParse(text, out DateOnly date) ? Format(date) : text;
    }
}
