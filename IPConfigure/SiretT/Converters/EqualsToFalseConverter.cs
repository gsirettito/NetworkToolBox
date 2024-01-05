namespace SiretT.Converters {
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Markup;

    /// <summary>
    /// Checks equality of value and the converter parameter.
    /// Returns <see cref="Visibility.Visible"/> if they are equal.
    /// Returns <see cref="Visibility.Collapsed"/> if they are NOT equal.
    /// </summary>
    public class EqualsToFalseConverter : IValueConverter {
        #region Implementation of IValueConverter

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value.ToString() == parameter.ToString()
                || (value != null && value.Equals(parameter))) {
                return false;
            }
            return true;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }

        #endregion
    }

    public interface INippleConverter : IValueConverter, INipple {}

    public interface INipple {
        INippleConverter NippleConverter { get; set; }
    }

    public class BooleanToVisibilityNippleConverter : INippleConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (NippleConverter != null) {
                value = NippleConverter.Convert(value, targetType, parameter, culture);
            }
            if ((bool)value) return Visibility.Visible;
            return Visibility.Collapsed;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;            
        }

        public INippleConverter NippleConverter {
            get;
            set;
        }
    }

    public class EqualsToFalseConverterNippleConverter : INippleConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (NippleConverter != null) {
                value = NippleConverter.Convert(value, targetType, parameter, culture);
            }
            if (value.ToString() == parameter.ToString()
                 || (value != null && value.Equals(parameter))) {
                return false;
            }
            return true;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }

        public INippleConverter NippleConverter {
            get;
            set;
        }
    }
}