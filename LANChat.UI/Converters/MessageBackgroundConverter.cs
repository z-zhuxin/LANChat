using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LANChat.Core.Models;

namespace LANChat.UI.Converters
{
    public class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Message message)
            {
                if (parameter as string == "alignment")
                {
                    return message.Type == MessageType.System
                        ? HorizontalAlignment.Center
                        : message.SenderId == App.Current.Properties["UserId"]?.ToString()
                            ? HorizontalAlignment.Right
                            : HorizontalAlignment.Left;
                }

                return message.Type == MessageType.System
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"))
                    : message.SenderId == App.Current.Properties["UserId"]?.ToString()
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD"))
                        : new SolidColorBrush(Colors.White);
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}