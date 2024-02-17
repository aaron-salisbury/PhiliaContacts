using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using System.IO;

namespace PhiliaContacts.Presentation.Base.Converters
{
    public class BytesToBitmapConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is byte[] imageBuffer && targetType.IsAssignableTo(typeof(IImageBrushSource)))
            {
                using MemoryStream ms = new(imageBuffer);
                return new Avalonia.Media.Imaging.Bitmap(ms);
            }
            else
            {
                return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
