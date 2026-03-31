using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Yggdrasil.Presentation.Converters
{
    /// <summary>
    /// Used to convert a boolean value to a Brush for UI elements. You can specify the brush to use for true and false values via the TrueBrush and FalseBrush properties.
    /// e.g. making duplicate bone slots assignments red
    /// </summary>
    public sealed class BooleanToBrushConverter : DependencyObject, IValueConverter
    {
        public Brush? TrueBrush
        {
            get => (Brush?)GetValue(TrueBrushProperty);
            set => SetValue(TrueBrushProperty, value);
        }

        public static readonly DependencyProperty TrueBrushProperty =
            DependencyProperty.Register(
                nameof(TrueBrush),
                typeof(Brush),
                typeof(BooleanToBrushConverter),
                new PropertyMetadata(null));

        public Brush? FalseBrush
        {
            get => (Brush?)GetValue(FalseBrushProperty);
            set => SetValue(FalseBrushProperty, value);
        }

        public static readonly DependencyProperty FalseBrushProperty =
            DependencyProperty.Register(
                nameof(FalseBrush),
                typeof(Brush),
                typeof(BooleanToBrushConverter),
                new PropertyMetadata(null));

        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool boolValue && boolValue ? TrueBrush : FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
