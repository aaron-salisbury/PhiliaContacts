using System;

namespace PhiliaContacts.Core.Base.Extensions
{
    public static class StringExtensions
    {
        public static string GetAfterLastOrEmpty(this string text, string stopAt)
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(stopAt))
            {
                int charLocation = text.LastIndexOf(stopAt) + stopAt.Length;

                if (charLocation > 0)
                {
                    return text.Substring(charLocation, text.Length - charLocation);
                }
            }

            return string.Empty;
        }

        public static string GetUntilOrEmpty(this string text, string stopAt)
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(stopAt))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }

            return string.Empty;
        }
    }
}
