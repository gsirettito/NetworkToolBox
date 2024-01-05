using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetworkToolBox {
    /// <summary>
    /// Lógica de interacción para IPAddressBox.xaml
    /// </summary>
    public partial class IPAddressBox : UserControl {
        private string _ipaddress;

        public IPAddressBox() {
            InitializeComponent();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e) {
            base.OnPreviewKeyDown(e);
            var vKey = KeyInterop.VirtualKeyFromKey(e.Key);
            var element = e.OriginalSource as TextBox;
            if (element == null) return;
            if (e.Key == Key.Left || e.Key == Key.Back) {
                if (element.CaretIndex == 0) {
                    MoveFocus(element, -1);
                    e.Handled = true;
                }
            } else if (e.Key == Key.Right) {
                if (element.CaretIndex == element.Text.Length) {
                    MoveFocus(element, 1);
                    e.Handled = true;
                }
            } else if ((vKey >= 48 && vKey <= 48 + 9)
                || (vKey >= 96 && vKey <= 105)) {
                if (element.CaretIndex == 3) {
                    MoveFocus(element, 1, true);
                    //e.Handled = true;
                }
            } else if (e.Key == Key.OemPeriod || e.Key == Key.Decimal) {
                MoveFocus(element, 1, true);
                e.Handled = true;
            }
        }

        public string IPAddress {
            get {
                return $"{oct1.Text}.{oct2.Text}.{oct3.Text}.{oct4.Text}";
            }
            set {
                if (value == _ipaddress) return;
                if (string.IsNullOrEmpty(value.Trim().Trim('.'))) {
                    _ipaddress = value;
                    oct1.Text = "";
                    oct2.Text = "";
                    oct3.Text = "";
                    oct4.Text = "";
                    return;
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^\b((2[0-4]\d|25[0-5]|1\d\d?|\d\d?)\.?\b){4}$")) throw new InvalidCastException();
                _ipaddress = value;

                var octs = value.Split('.');
                oct1.Text = octs[0];
                oct2.Text = octs[1];
                oct3.Text = octs[2];
                oct4.Text = octs[3];
            }
        }

        private void MoveFocus(TextBox element, int direction, bool selectAll) {
            if (element.Name == "oct1" && direction == -1) return;
            else if (element.Name == "oct2" && direction == -1) element = oct1;
            else if (element.Name == "oct3" && direction == -1) element = oct2;
            else if (element.Name == "oct4" && direction == -1) element = oct3;
            else if (element.Name == "oct1" && direction == 1) element = oct2;
            else if (element.Name == "oct2" && direction == 1) element = oct3;
            else if (element.Name == "oct3" && direction == 1) element = oct4;
            else if (element.Name == "oct4" && direction == 1) return;

            element.Focus();
            element.CaretIndex = direction < 0 ? element.Text.Length : 0;
            if (selectAll) element.SelectAll();
        }

        private void MoveFocus(TextBox element, int direction) {
            MoveFocus(element, direction, false);
        }
    }
}
