using System.Globalization;
using System.Windows.Data;

namespace FlbxStudio.Converters;

/// <summary>
/// Converts a boolean value to Visibility: True becomes Collapsed/Hidden, False becomes Visible.
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to inverse visibility.
    /// </summary>
    /// <param name="value">The source value expected to be a boolean.</param>
    /// <param name="targetType">The target binding type.</param>
    /// <param name="parameter">Optional converter parameter.</param>
    /// <param name="culture">The culture used in the converter.</param>
    /// <returns><see cref="Visibility.Collapsed"/> when true; otherwise <see cref="Visibility.Visible"/>.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible; // Default fallback if data context is uninitialized
    }

    /// <summary>
    /// Converts a visibility value back to the inverse boolean representation.
    /// </summary>
    /// <param name="value">The source value expected to be a <see cref="Visibility"/>.</param>
    /// <param name="targetType">The target binding type.</param>
    /// <param name="parameter">Optional converter parameter.</param>
    /// <param name="culture">The culture used in the converter.</param>
    /// <returns><see langword="true"/> when visibility is not visible; otherwise <see langword="false"/>.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}