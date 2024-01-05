using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetworkToolBox {
    public class ByteBox : TextBox {
        private List<Key> permitedKeys = new List<Key>() {
            Key.C, Key.X, Key.V, Key.Tab,
            Key.Home, Key.End, Key.Enter, Key.Return,
            Key.Delete, Key.Left,
            Key.Right, Key.NumPad0, Key.NumPad1,
            Key.NumPad2, Key.NumPad3, Key.NumPad4,
            Key.NumPad5, Key.NumPad6, Key.NumPad7,
            Key.NumPad8, Key.NumPad9,
            Key.D0, Key.D1, Key.D2, Key.D3, Key.D4,
            Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,
            Key.Back };
        private string lastText;
        private int lastCaret;

        protected override void OnTextChanged(TextChangedEventArgs e) {
            base.OnTextChanged(e);

        }
        public ByteBox() {
            TextChanged += ByteBox_TextChanged;
        }

        private void ByteBox_TextChanged(object sender, TextChangedEventArgs e) {
            byte value;
            if (Text == "" || byte.TryParse(this.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value)) {
                lastText = Text;
                lastCaret = CaretIndex;
            } else {
                TextChanged -= ByteBox_TextChanged;
                Text = lastText;
                CaretIndex = lastCaret;
                TextChanged += ByteBox_TextChanged;
            }
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e) {
            base.OnPreviewKeyDown(e);
            if (permitedKeys.Contains(e.Key)) {
                double clipb;
                if (e.Key == Key.V && (Keyboard.Modifiers != ModifierKeys.Control
                    || !double.TryParse(Clipboard.GetText(), out clipb)
                    || !double.TryParse(Text.Insert(CaretIndex, Clipboard.GetText()), out clipb))) e.Handled = true;
                if (e.Key == Key.C && Keyboard.Modifiers != ModifierKeys.Control) e.Handled = true;
                if (e.Key == Key.X && Keyboard.Modifiers != ModifierKeys.Control) e.Handled = true;
            } else e.Handled = true;
        }
    }
}
