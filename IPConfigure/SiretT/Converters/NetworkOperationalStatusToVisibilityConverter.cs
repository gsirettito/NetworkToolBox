using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Data;

namespace SiretT.Converters {
    public sealed class NetworkOperationalStatusToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) return Visibility.Collapsed;
            return (value is OperationalStatus && ((OperationalStatus)value | OperationalStatus.Up) == OperationalStatus.Up) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }
    }
}
