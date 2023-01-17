using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GraphSharp.Controls
{
    /// <summary>
    /// 提供将任意控件裁剪为圆形或椭圆的附加属性。
    /// </summary>
    public static class EllipseClipper
    {
        /// <summary>
        /// 标识 IsClipping 的附加属性。
        /// </summary>
        public static readonly DependencyProperty IsClippingProperty = DependencyProperty.RegisterAttached(
            "IsClipping", typeof(bool), typeof(EllipseClipper), new PropertyMetadata(false, OnIsClippingChanged));

        public static void SetIsClipping(DependencyObject element, bool value)
            => element.SetValue(IsClippingProperty, value);

        public static bool GetIsClipping(DependencyObject element)
            => (bool)element.GetValue(IsClippingProperty);

        private static void OnIsClippingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (UIElement)d;
            if (e.NewValue is false)
            {
                // 如果 IsClipping 附加属性被设置为 false，则清除 UIElement.Clip 属性。
                source.ClearValue(UIElement.ClipProperty);
                return;
            }

            // 如果 UIElement.Clip 属性被用作其他用途，则抛出异常说明问题所在。
            var ellipse = source.Clip as EllipseGeometry;
            if (source.Clip != null && ellipse == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(EllipseClipper).FullName}.{IsClippingProperty.Name} " +
                    $"is using {source.GetType().FullName}.{UIElement.ClipProperty.Name} " +
                    "for clipping, dont use this property manually.");
            }

            // 使用 UIElement.Clip 属性。
            ellipse = ellipse ?? new EllipseGeometry();
            source.Clip = ellipse;

            // 使用绑定来根据控件的宽高更新椭圆裁剪范围。
            var xBinding = new Binding(FrameworkElement.ActualWidthProperty.Name)
            {
                Source = source,
                Mode = BindingMode.OneWay,
                Converter = new HalfConverter(),
            };
            var yBinding = new Binding(FrameworkElement.ActualHeightProperty.Name)
            {
                Source = source,
                Mode = BindingMode.OneWay,
                Converter = new HalfConverter(),
            };
            var xyBinding = new MultiBinding
            {
                Converter = new SizeToClipCenterConverter(),
            };
            xyBinding.Bindings.Add(xBinding);
            xyBinding.Bindings.Add(yBinding);
            BindingOperations.SetBinding(ellipse, EllipseGeometry.RadiusXProperty, xBinding);
            BindingOperations.SetBinding(ellipse, EllipseGeometry.RadiusYProperty, yBinding);
            BindingOperations.SetBinding(ellipse, EllipseGeometry.CenterProperty, xyBinding);
        }

        private sealed class SizeToClipCenterConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
                => new Point((double)values[0], (double)values[1]);

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }

        private sealed class HalfConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                =>  ((double)value)/2.0;

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
