using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FlbxStudio.Converters;

/// <summary>
/// Converts raw image byte arrays to <see cref="BitmapImage"/> instances for UI binding.
/// </summary>
public class BytesToImageConverter : IValueConverter
{
    /// <summary>
    /// Converts a raw image byte array to a frozen bitmap image.
    /// </summary>
    /// <param name="value">The source value expected to be a byte array.</param>
    /// <param name="targetType">The target binding type.</param>
    /// <param name="parameter">Optional converter parameter.</param>
    /// <param name="culture">The culture used in the converter.</param>
    /// <returns>A <see cref="BitmapImage"/> when conversion succeeds; otherwise <see langword="null"/>.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            try
            {
                var image = new BitmapImage();
                using (var mem = new MemoryStream(bytes))
                {
                    image.BeginInit();
                    image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    image.CacheOption = BitmapCacheOption.OnLoad; // Completely releases the memory stream after loading
                    image.StreamSource = mem;
                    image.EndInit();
                }
                image.Freeze(); // Makes the image cross-thread safe and optimizes rendering speed
                return image;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Not implemented because conversion from image back to bytes is not required by this UI.
    /// </summary>
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="targetType">The source binding type.</param>
    /// <param name="parameter">Optional converter parameter.</param>
    /// <param name="culture">The culture used in the converter.</param>
    /// <returns>This method always throws.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}