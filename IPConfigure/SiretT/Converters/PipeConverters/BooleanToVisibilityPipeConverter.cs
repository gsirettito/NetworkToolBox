using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SiretT.Converters {
    public class BooleanToVisibilityPipeConverter : IPipeConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (PipeConverter != null) {
                value = PipeConverter.Convert(value, targetType, parameter, culture);
            }
            if ((bool)value) return Visibility.Visible;
            return Visibility.Collapsed;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }

        public IPipeConverter PipeConverter {
            get;
            set;
        }
    }
}