using MS.Internal;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace SiretT.Fonts {

    public enum FontAwesomePack {
        Solid, Brands, Sharp, Regular
    }

    [ContentProperty("Icon")]
    internal class FontAwesomeIcon : ContentControl {
        public FontAwesomeIcon() {
            fonts = FontAwesomeSelector.Parse(SiretT.Fonts.Resources.css);
            brandsUri = new Uri("pack://application:,,,/SiretT/Fonts/fa-brands-400.ttf");
            solidUri = new Uri("pack://application:,,,/SiretT/Fonts/fa-solid-900.ttf");
            sharpUri = new Uri("pack://application:,,,/SiretT/Fonts/fa-sharp-solid-900.ttf");
            brandsFamilyName = "pack://application:,,,/SiretT/Fonts/#Font Awesome 6 Brands Regular";
            solidFamilyName = "pack://application:,,,/SiretT/Fonts/#Font Awesome 6 Pro Solid";
            sharpFamilyName = "pack://application:,,,/SiretT/Fonts/#Font Awesome 6 Sharp Solid";
        }

        public static readonly DependencyProperty FontPackProperty = DependencyProperty.Register(
                    "FontPack", typeof(FontAwesomePack), typeof(FontAwesomeIcon), new PropertyMetadata(FontAwesomePack.Solid, OnPropertyChange));

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            "Icon", typeof(string), typeof(FontAwesomeIcon), new PropertyMetadata(OnPropertyChange));

        private FontAwesomeSelector fonts;
        private Uri brandsUri;
        private Uri solidUri;
        private Uri sharpUri;
        private string brandsFamilyName;
        private string solidFamilyName;
        private string sharpFamilyName;

        private static void OnPropertyChange(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var self = d as FontAwesomeIcon;
            if (e.Property == IconProperty) {
                switch (self.FontPack) {
                    case FontAwesomePack.Solid: self.FontFamily = new System.Windows.Media.FontFamily(self.solidUri, self.solidFamilyName); break;
                    case FontAwesomePack.Sharp: self.FontFamily = new System.Windows.Media.FontFamily(self.sharpUri, self.sharpFamilyName); break;
                    case FontAwesomePack.Brands: self.FontFamily = new System.Windows.Media.FontFamily(self.brandsUri, self.brandsFamilyName); break;
                }
                try {
                    if (self.fonts.Classes.ContainsKey(self.Icon)) {
                        var ico = (char)self.fonts.Classes[self.Icon];
                        self.Content = ico.ToString();
                    } else self.Content = self.Icon;
                } catch { }
            } else if (e.Property == FontPackProperty) {
                switch ((FontAwesomePack)e.NewValue) {
                    case FontAwesomePack.Solid: self.FontFamily = new System.Windows.Media.FontFamily(self.solidUri, self.solidFamilyName); break;
                    case FontAwesomePack.Sharp: self.FontFamily = new System.Windows.Media.FontFamily(self.sharpUri, self.sharpFamilyName); break;
                    case FontAwesomePack.Brands: self.FontFamily = new System.Windows.Media.FontFamily(self.brandsUri, self.brandsFamilyName); break;
                }
            }
        }

        public string Icon {
            get {
                return GetValue(IconProperty).ToString();
            }
            set { SetValue(IconProperty, value); }
        }

        public FontAwesomePack FontPack {
            get { return (FontAwesomePack)GetValue(FontPackProperty); }
            set { SetValue(FontPackProperty, value); }
        }
    }

    internal class FontAwesomeSelector {
        public Dictionary<string, int> Classes { get; private set; }

        public static FontAwesomeSelector Parse(string css) {
            FontAwesomeSelector selector = new FontAwesomeSelector();
            selector.Classes = new Dictionary<string, int>();
            var fa_content = Regex.Matches(css, @"\.fa.+\n.+content:.+\n\}");
            int l1 = "content: \"".Length;
            foreach (Match i in fa_content) {
                var content = Regex.Match(i.Value, @"content: "".+""");
                if (content.Success) {
                    var value = Convert.ToInt32(content.Value.Substring(l1, content.Length - l1 - 1).Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0], 16);
                    var l0 = i.Value.Split('\n')[0];
                    var classes = l0.Substring(0, l0.Length - 2).Trim().Split(',');
                    foreach (var c in classes) {
                        var name = c.Split(':')[0].Substring(1);
                        if (!selector.Classes.ContainsKey(name))
                            selector.Classes.Add(name, value);
                    }
                }

            }
            return selector;
        }
    }
}