using Avalonia.Data;
using Avalonia.Data.Converters;
using Material.Icons;
using System;
using System.Globalization;

namespace PhiliaContacts.Presentation.Desktop.Base.Converters;

/// <summary>
/// Converts a boolean value to a <see cref="MaterialIconKind"/>.
/// </summary>
public class BooleanToMaterialIconKindConverter : IValueConverter
{
    private const MaterialIconKind DefaultTrueIcon = MaterialIconKind.Image;
    private const MaterialIconKind DefaultFalseIcon = MaterialIconKind.ImageOffOutline;

    /// <summary>
    /// Converts a boolean value to a configured <see cref="MaterialIconKind"/>.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">
    /// Optional icon configuration.
    /// <list type="bullet">
    /// <item><description><see cref="MaterialIconKind"/>: icon used for <see langword="true"/>; false uses an <c>Outline</c> variant when available.</description></item>
    /// <item><description><see cref="string"/> icon name (for example, <c>"Heart"</c>): same behavior as enum input.</description></item>
    /// <item><description><see cref="string"/> pair (for example, <c>"Heart|HeartOutline"</c> or <c>"Heart,HeartOutline"</c>): explicit true/false icons.</description></item>
    /// </list>
    /// </param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A <see cref="MaterialIconKind"/> based on the boolean input and optional parameter;
    /// otherwise a <see cref="BindingNotification"/> when input type is unsupported.
    /// </returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool flag)
        {
            return new BindingNotification(new InvalidCastException("Value must be a boolean."), BindingErrorType.Error);
        }

        if (!TryResolveIcons(parameter, out MaterialIconKind trueIcon, out MaterialIconKind falseIcon, out Exception? error))
        {
            return new BindingNotification(error ?? new InvalidCastException("Unable to resolve icon parameter."), BindingErrorType.Error);
        }

        return flag ? trueIcon : falseIcon;
    }

    /// <summary>
    /// Not supported. Throws <see cref="NotSupportedException"/> if called.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">An optional parameter.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>This method does not return a value.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static bool TryResolveIcons(object? parameter, out MaterialIconKind trueIcon, out MaterialIconKind falseIcon, out Exception? error)
    {
        trueIcon = DefaultTrueIcon;
        falseIcon = DefaultFalseIcon;
        error = null;

        if (parameter is null)
        {
            return true;
        }

        if (parameter is MaterialIconKind iconKind)
        {
            trueIcon = iconKind;
            falseIcon = ResolveOutlineIcon(iconKind);
            return true;
        }

        if (parameter is string text)
        {
            string[] parts = text.Split(['|', ',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                if (!Enum.TryParse(parts[0], true, out MaterialIconKind parsed))
                {
                    error = new InvalidCastException($"Unknown MaterialIconKind '{parts[0]}'.");
                    return false;
                }

                trueIcon = parsed;
                falseIcon = ResolveOutlineIcon(parsed);
                return true;
            }

            if (parts.Length == 2)
            {
                if (!Enum.TryParse(parts[0], true, out MaterialIconKind parsedTrue))
                {
                    error = new InvalidCastException($"Unknown MaterialIconKind '{parts[0]}'.");
                    return false;
                }

                if (!Enum.TryParse(parts[1], true, out MaterialIconKind parsedFalse))
                {
                    error = new InvalidCastException($"Unknown MaterialIconKind '{parts[1]}'.");
                    return false;
                }

                trueIcon = parsedTrue;
                falseIcon = parsedFalse;
                return true;
            }

            error = new InvalidCastException("Icon parameter string must contain one or two icon names.");
            return false;
        }

        error = new InvalidCastException("Parameter must be MaterialIconKind or icon name string.");
        return false;
    }

    private static MaterialIconKind ResolveOutlineIcon(MaterialIconKind iconKind)
    {
        string iconName = iconKind.ToString();

        if (iconName.EndsWith("Outline", StringComparison.Ordinal))
        {
            return iconKind;
        }

        if (Enum.TryParse($"{iconName}Outline", out MaterialIconKind outline))
        {
            return outline;
        }

        return DefaultFalseIcon;
    }
}
