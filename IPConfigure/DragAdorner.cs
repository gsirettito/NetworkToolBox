using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace NetworkToolBox {
    /// <summary>
    /// An adorner class that contains a floating Text into a canvas to provide 
    /// the ability to move elements for a TreeView control.
    /// </summary>
    internal sealed class DragAdorner : Adorner {

        #region Private Variables
        //Visual children
        private VisualCollection _visualChildren;
        private TreeViewItem _currentTvi;
        private bool _isVisible;
        private Point _startPos;
        private Canvas _canvas;
        private TreeViewItem _lastTvi;
        private ItemsControl _tviParent;
        private int _tviIndex;
        private UIElement _adornedElement;
        private TextBlock _floatingText;
        private bool isDragging;
        private bool cancelDragging;
        #endregion

        public DragAdorner(UIElement adornedElement, UIElement adorningElement) : base(adornedElement) {
            _currentTvi = adorningElement as TreeViewItem;
            _tviParent = _currentTvi.Parent as ItemsControl;
            _tviIndex = _tviParent.Items.IndexOf(_currentTvi);
            _adornedElement = adornedElement;
            _adornedElement.PreviewKeyDown += _canvas_KeyDown;

            Debug.Assert(_currentTvi != null, "No TreeViewItem");

            _visualChildren = new VisualCollection(this);

            BuildFloatingItem();
        }

        #region Public Methods

        /// <summary>
        /// Specifies whether a Layer is visible.
        /// </summary>
        /// <param name="isVisible"></param>
        public void UpdateVisibilty(bool isVisible) {
            _isVisible = isVisible;
            InvalidateMeasure();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Override to measure elements.
        /// </summary>
        protected override Size MeasureOverride(Size constraint) {
            if (_isVisible) {
                AdornedElement.Measure(constraint);

                return new Size(AdornedElement.RenderSize.Width,
                                AdornedElement.RenderSize.Height);
            } else  //if it is not in editable mode, no need to show anything.
                return new Size(0, 0);
        }

        /// <summary>
        /// override function to arrange elements.
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize) {
            if (_isVisible) {
                _canvas.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            } else // if is not is editable mode, no need to show elements.
              {
                _canvas.Arrange(new Rect(0, 0, 0, 0));
            }
            return finalSize;
        }

        /// <summary>
        /// override property to return infomation about visual tree.
        /// </summary>
        protected override int VisualChildrenCount {
            get { return _visualChildren.Count; }
        }

        /// <summary>
        /// override function to return infomation about visual tree.
        /// </summary>
        protected override Visual GetVisualChild(int index) { return _visualChildren[index]; }

        #endregion

        #region Private Methods

        private void BuildFloatingItem() {
            // Building a canvas and a floating Text based on TextBlock with mouse move tracking
            _startPos = Mouse.GetPosition(_adornedElement);
            _canvas = new Canvas();
            _canvas.Background = Brushes.Transparent;
            _floatingText = new TextBlock() {
                Background = Brushes.LightYellow,
                Padding = new Thickness(6, 3, 6, 3),
                Text = _currentTvi.Header.ToString(),
                Visibility = Visibility.Collapsed
            };
            _floatingText.PreviewKeyDown += _canvas_KeyDown;
            _canvas.Children.Add(_floatingText);
            _canvas.MouseEnter += _canvas_MouseEnter;
            _canvas.MouseMove += _canvas_MouseMove;
            _canvas.PreviewMouseUp += _canvas_PreviewMouseUp;
            //_canvas.LayoutUpdated += new EventHandler(OnLayoutUpdated);
            _visualChildren.Add(_canvas);
        }

        private void OnLayoutUpdated(object sender, EventArgs e) {
            if (_isVisible)
                _canvas.Focus();
        }

        private void _canvas_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (isDragging && e.Key == System.Windows.Input.Key.Escape) {
                cancelDragging = true;
                StopDrag();
            }
        }

        private void _canvas_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) {
                StopDrag();
            }
        }

        private void _canvas_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            StopDrag();

            var pos = e.GetPosition(this);
            object element = AdornedElement.InputHitTest(pos);

            while (element.GetType() != typeof(TreeViewItem) && element.GetType() != typeof(TreeView)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }

            if (_lastTvi != null) {
                _lastTvi.BorderThickness = new Thickness(0, 0, 0, 0);
                _lastTvi.BorderBrush = null;
                _lastTvi.Background = Brushes.Transparent;
            }

            if (element is TreeViewItem && (element as TreeViewItem).Header is Container) {
                if (_lastTvi != null && _lastTvi != _currentTvi) {
                    _tviParent.Items.RemoveAt(_tviIndex);
                    _lastTvi.Items.Add(_currentTvi);
                    _currentTvi.IsSelected = true;
                }
            } else if (element is TreeView) {
                _tviParent.Items.RemoveAt(_tviIndex);
                (element as ItemsControl).Items.Add(_currentTvi);
                _currentTvi.IsSelected = true;
            }
        }

        private void StopDrag() {
            isDragging = false;
            UpdateVisibilty(false);
            _canvas.MouseMove -= _canvas_MouseMove;
            _adornedElement.PreviewKeyDown -= _canvas_KeyDown;
            _visualChildren.Clear();
            if (_lastTvi != null) {
                _lastTvi.BorderThickness = new Thickness(0, 0, 0, 0);
                _lastTvi.BorderBrush = null;
                _lastTvi.Background = Brushes.Transparent;
            }
        }

        private void _canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            var pos = e.GetPosition(this);
            if (_startPos == pos) return;
            if (!isDragging) {
                isDragging = true;
                var background = new SolidColorBrush(Color.FromArgb(15, 0x9a, 0xcd, 0x32));
                _canvas.Background = background;
                _floatingText.Visibility = Visibility.Visible;
            }

            if (cancelDragging) StopDrag();
            Canvas.SetLeft(_floatingText, pos.X + SystemParameters.CursorHeight / 2);
            Canvas.SetTop(_floatingText, pos.Y + SystemParameters.CursorHeight / 2);

            object element = AdornedElement.InputHitTest(pos);

            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }
            var _tvi = element as TreeViewItem;

            if (_tvi != null) {
                if (_lastTvi != null && _lastTvi != _tvi) {
                    _lastTvi.BorderThickness = new Thickness(0, 0, 0, 0);
                    _lastTvi.BorderBrush = null;
                    _lastTvi.Background = Brushes.Transparent;
                };

                if (_tvi.Header is Container && _tvi != _currentTvi) {
                    _tvi.BorderThickness = new Thickness(0, 0, 0, 2);
                    _tvi.BorderBrush = Brushes.Black;
                    _tvi.Background = Brushes.AliceBlue;
                }
                _lastTvi = _tvi;
            }
        }
        #endregion
    }
}