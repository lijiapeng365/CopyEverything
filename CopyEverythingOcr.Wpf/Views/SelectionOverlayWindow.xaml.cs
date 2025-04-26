using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

// Explicitly use WPF Input namespace
using WpfInput = System.Windows.Input;

namespace CopyEverythingOcr.Wpf.Views
{
    /// <summary>
    /// Interaction logic for SelectionOverlayWindow.xaml
    /// </summary>
    public partial class SelectionOverlayWindow : Window
    {
        // Use explicit System.Windows types to avoid ambiguity
        private System.Windows.Point _startPoint;
        // Mark as nullable because it's initialized later
        private System.Windows.Shapes.Rectangle? _selectionRectangle;
        private bool _isSelecting = false;

        // Public property to access the selected region after the window closes
        public Rect SelectedRegion { get; private set; } = Rect.Empty;

        public SelectionOverlayWindow()
        {
            InitializeComponent();
            AdjustWindowToBounds(); // Adjust window to cover all screens
        }

        private void AdjustWindowToBounds()
        {
             // Get the virtual screen dimensions (covers all monitors)
             double virtualScreenWidth = SystemParameters.VirtualScreenWidth;
             double virtualScreenHeight = SystemParameters.VirtualScreenHeight;
             double virtualScreenLeft = SystemParameters.VirtualScreenLeft;
             double virtualScreenTop = SystemParameters.VirtualScreenTop;

             this.Left = virtualScreenLeft;
             this.Top = virtualScreenTop;
             this.Width = virtualScreenWidth;
             this.Height = virtualScreenHeight;
             this.WindowState = WindowState.Normal; // Ensure it uses the set dimensions
        }


        // Use explicit WpfInput.MouseButtonEventArgs
        protected override void OnMouseLeftButtonDown(WpfInput.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _startPoint = e.GetPosition(this); // Use 'this' for coordinates relative to the window
            _isSelecting = true;

            // Remove previous rectangle if any
            if (_selectionRectangle != null)
            {
                SelectionCanvas.Children.Remove(_selectionRectangle);
            }

            // Use explicit System.Windows.Shapes.Rectangle
            _selectionRectangle = new System.Windows.Shapes.Rectangle
            {
                // Explicitly use System.Windows.Media.Brushes
                Stroke = System.Windows.Media.Brushes.LightSkyBlue,
                StrokeThickness = 2,
                // Explicitly use System.Windows.Media.Color
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 120, 215))
            };

            Canvas.SetLeft(_selectionRectangle, _startPoint.X);
            Canvas.SetTop(_selectionRectangle, _startPoint.Y);
            SelectionCanvas.Children.Add(_selectionRectangle);

            // Capture mouse to ensure we get MouseUp even if cursor leaves window
            this.CaptureMouse();
        }

        // Use explicit WpfInput.MouseEventArgs
        protected override void OnMouseMove(WpfInput.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isSelecting && _selectionRectangle != null)
            {
                // Use explicit System.Windows.Point
                System.Windows.Point currentPoint = e.GetPosition(this);

                double width = Math.Abs(currentPoint.X - _startPoint.X);
                double height = Math.Abs(currentPoint.Y - _startPoint.Y);
                double left = Math.Min(currentPoint.X, _startPoint.X);
                double top = Math.Min(currentPoint.Y, _startPoint.Y);

                _selectionRectangle.Width = width;
                _selectionRectangle.Height = height;
                Canvas.SetLeft(_selectionRectangle, left);
                Canvas.SetTop(_selectionRectangle, top);
            }
        }

        // Use explicit WpfInput.MouseButtonEventArgs
        protected override void OnMouseLeftButtonUp(WpfInput.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_isSelecting)
            {
                _isSelecting = false;
                this.ReleaseMouseCapture(); // Release mouse capture

                // Use explicit System.Windows.Point
                System.Windows.Point endPoint = e.GetPosition(this);

                 // Calculate the selected rectangle in window coordinates
                 double left = Math.Min(_startPoint.X, endPoint.X);
                 double top = Math.Min(_startPoint.Y, endPoint.Y);
                 double width = Math.Abs(endPoint.X - _startPoint.X);
                 double height = Math.Abs(endPoint.Y - _startPoint.Y);


                if (width > 0 && height > 0)
                {
                    // Convert window coordinates to screen coordinates
                    PresentationSource source = PresentationSource.FromVisual(this);
                    double dpiX = 1.0, dpiY = 1.0;
                    if (source?.CompositionTarget != null)
                    {
                        dpiX = source.CompositionTarget.TransformToDevice.M11; // DPI scaling factor X
                        dpiY = source.CompositionTarget.TransformToDevice.M22; // DPI scaling factor Y
                    }

                    // Calculate screen coordinates considering DPI
                    // Note: Left and Top from the window are already in device-independent units (like WPF coords)
                    double screenLeft = (this.Left + left) * dpiX;
                    double screenTop = (this.Top + top) * dpiY;
                    double screenWidth = width * dpiX;
                    double screenHeight = height * dpiY;

                    // Store the selected region in screen coordinates (as a Rect for convenience)
                    SelectedRegion = new Rect(screenLeft, screenTop, screenWidth, screenHeight);
                    this.DialogResult = true; // Indicate selection was successful
                }
                else
                {
                    SelectedRegion = Rect.Empty;
                    this.DialogResult = false; // Indicate no valid selection or cancellation
                }

                this.Close(); // Close the overlay window
            }
        }

        // Handle ESC key press to cancel selection
        // Use explicit WpfInput.KeyEventArgs and WpfInput.Key
        private void Window_KeyDown(object sender, WpfInput.KeyEventArgs e)
        {
            if (e.Key == WpfInput.Key.Escape)
            {
                _isSelecting = false; // Stop selection process if ongoing
                this.ReleaseMouseCapture();
                SelectedRegion = Rect.Empty;
                this.DialogResult = false; // Indicate cancellation
                this.Close();
            }
        }
    }
} 