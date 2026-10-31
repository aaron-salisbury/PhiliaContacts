using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace PhiliaContacts.Presentation.Desktop.Base.Converters;

/// <summary>
/// Converts image values between <see cref="byte[]"/> buffers and <see cref="Bitmap"/> instances for data binding.
/// </summary>
public class BytesToBitmapConverter : IValueConverter
{
    // Decoded bitmaps live as long as their source buffer, so re-realized list items reuse them instead of decoding again.
    // Keys are the byte[] instances, so a cache entry is released once the owning contact is no longer referenced.
    private readonly ConditionalWeakTable<byte[], Bitmap> _cache = [];

    /// <summary>
    /// Converts a source value to a <see cref="Bitmap"/> when possible.
    /// </summary>
    /// <param name="value">
    /// The source value. Supported values are <see langword="null"/>, <see cref="Bitmap"/>, and <see cref="byte[]"/>.
    /// </param>
    /// <param name="targetType">The expected target type for the binding result.</param>
    /// <param name="parameter">
    /// An optional decode width in pixels. When given, the image is decoded at that width (preserving aspect ratio), which keeps
    /// thumbnails from holding full-resolution bitmaps in memory.
    /// </param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A <see cref="Bitmap"/> when conversion succeeds, <see langword="null"/> when <paramref name="value"/> is <see langword="null"/>,
    /// or a <see cref="BindingNotification"/> containing an error when conversion fails.
    /// </returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType != typeof(object) && (!typeof(IImage).IsAssignableFrom(targetType) && !typeof(IImageBrushSource).IsAssignableFrom(targetType)))
        {
            return new BindingNotification(new InvalidCastException($"Target type must be assignable to either {nameof(IImage)} or {nameof(IImageBrushSource)}."), BindingErrorType.Error);
        }

        if (value == null)
        {
            return null;
        }
        else if (value is Bitmap bitmap)
        {
            return bitmap;
        }
        else if (value is byte[] imageBuffer)
        {
            if (_cache.TryGetValue(imageBuffer, out Bitmap? cached))
            {
                return cached;
            }

            try
            {
                using MemoryStream ms = new(imageBuffer);
                Bitmap decoded = int.TryParse(parameter?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int decodeWidth) && decodeWidth > 0
                    ? Bitmap.DecodeToWidth(ms, decodeWidth)
                    : new Bitmap(ms);
                _cache.AddOrUpdate(imageBuffer, decoded);
                return decoded;
            }
            catch (Exception ex)
            {
                return new BindingNotification(ex, BindingErrorType.Error);
            }
        }
        else
        {
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }
    }

    /// <summary>
    /// Converts a source value to a <see cref="byte[]"/> image buffer when possible.
    /// </summary>
    /// <param name="value">
    /// The source value. Supported values are <see langword="null"/>, <see cref="byte[]"/>, and <see cref="Bitmap"/>.
    /// </param>
    /// <param name="targetType">The expected target type for the binding result.</param>
    /// <param name="parameter">
    /// An optional encoder hint. Use <c>"png"</c> (case-insensitive) to encode as PNG; any other value encodes as JPEG.
    /// </param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A <see cref="byte[]"/> when conversion succeeds, <see langword="null"/> when <paramref name="value"/> is <see langword="null"/>,
    /// or a <see cref="BindingNotification"/> containing an error when conversion fails.
    /// </returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType != typeof(object) && targetType != typeof(byte[]))
        {
            return new BindingNotification(new InvalidCastException("Target type must be byte[]."), BindingErrorType.Error);
        }

        if (value == null)
        {
            return null;
        }
        else if (value is byte[] imageBuffer)
        {
            return imageBuffer;
        }
        else if (value is Bitmap bitmap)
        {
            try
            {
                using MemoryStream ms = new();

                if (string.Equals(parameter?.ToString(), "png", StringComparison.OrdinalIgnoreCase))
                {
                    bitmap.Save(ms, PngBitmapEncoderOptions.Default);
                }
                else
                {
                    bitmap.Save(ms, JpegBitmapEncoderOptions.Default);
                }

                return ms.ToArray();
            }
            catch (Exception ex)
            {
                return new BindingNotification(ex, BindingErrorType.Error);
            }
        }
        else
        {
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }
    }
}
