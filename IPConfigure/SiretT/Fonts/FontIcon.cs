using System.Windows;
using System.Windows.Controls;

namespace SiretT.Fonts {
    internal class FontIcon : ContentControl {
        public static readonly DependencyProperty GlypProperty = DependencyProperty.Register(
            "Glyph", typeof(string), typeof(FontIcon), new PropertyMetadata(OnPropertyChange));

        private static void OnPropertyChange(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var self = d as FontIcon;
            if (e.Property == GlypProperty) {
                self.Content = e.NewValue;
            }
        }

        public string Glyph {
            get {
                return GetValue(GlypProperty).ToString();
            }
            set { SetValue(GlypProperty, value); }
        }
    }
}