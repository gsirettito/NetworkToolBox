using System;
using System.Globalization;
using System.Windows.Data;

namespace SiretT.Converters {
    public class EqualsToFalsePipeConverter : IPipeConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (PipeConverter != null) {
                value = PipeConverter.Convert(value, targetType, parameter, culture);
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

        public IPipeConverter PipeConverter {
            get;
            set;
        }
    }
}