using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Yggdrasil.Presentation.Converters;

public sealed class ThicknessScaleConverter : IValueConverter
{
    public double Scale { get; set; } = 1.0;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Thickness thickness)
        {
            return new Thickness(
                thickness.Left * Scale,
                thickness.Top * Scale,
                thickness.Right * Scale,
                thickness.Bottom * Scale);
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
