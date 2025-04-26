using System.Windows;
using System.Windows.Input;

namespace CopyEverythingOcr.Wpf.Views
{
    public partial class ResultWindow : Window
    {
        public ResultWindow()
        {
            InitializeComponent();
        }

        // Public property to set the result text
        public string ResultText
        {
            set { ResultTextBox.Text = value; }
        }

        // Allow dragging the window
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // Copy button click handler
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(ResultTextBox.Text))
                {
                    // Explicitly use System.Windows.Clipboard
                    System.Windows.Clipboard.SetText(ResultTextBox.Text);
                }
            }
            catch (Exception ex)
            {
                 // Explicitly use System.Windows.MessageBox
                 System.Windows.MessageBox.Show($"Failed to copy text to clipboard: {ex.Message}", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Close button click handler
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Positions the window near the specified screen rectangle.
        /// Tries to place it to the right, or below if space is limited.
        /// </summary>
        /// <param name="targetRect">The screen rectangle (e.g., the captured region).</param>
        public void PositionNear(Rect targetRect)
        {
            // Ensure window measurement is done before positioning
            this.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            this.Arrange(new Rect(this.DesiredSize));

            double windowWidth = this.ActualWidth;
            double windowHeight = this.ActualHeight;

            // Get screen working area (excludes taskbar)
            // Note: This might need refinement for multi-monitor setups where targetRect
            // could be on a different monitor than the primary one.
            // For simplicity, we use the primary screen's working area.
            //var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen; // Requires WinForms ref
            //Rect workingArea = new Rect(primaryScreen.WorkingArea.X, primaryScreen.WorkingArea.Y, primaryScreen.WorkingArea.Width, primaryScreen.WorkingArea.Height);
            // Simpler approach: use SystemParameters for primary screen work area
            Rect workingArea = SystemParameters.WorkArea;


            // Try placing to the right of the target rectangle
            double desiredLeft = targetRect.Right + 5;
            double desiredTop = targetRect.Top;

            // If it goes off the right edge, try placing below
            if (desiredLeft + windowWidth > workingArea.Right)
            {
                desiredLeft = targetRect.Left;
                desiredTop = targetRect.Bottom + 5;

                // If placing below also goes off the bottom edge, place it above
                if (desiredTop + windowHeight > workingArea.Bottom)
                {                    
                    desiredTop = targetRect.Top - windowHeight - 5;
                    // If placing above goes off the top edge, just clamp to top
                    if (desiredTop < workingArea.Top) 
                    { 
                         desiredTop = workingArea.Top;
                         // As a last resort if still overlapping, try placing left
                         if(targetRect.Left - windowWidth - 5 > workingArea.Left)
                         {
                              desiredLeft = targetRect.Left - windowWidth - 5;
                         } else {
                              // Or just align left edge with target if can't fit left
                              desiredLeft = targetRect.Left;
                         }
                    } 
                }
                 // Make sure it doesn't go off the left edge when placed below/above
                 if (desiredLeft + windowWidth > workingArea.Right)
                 {
                     desiredLeft = workingArea.Right - windowWidth;
                 }
                 if (desiredLeft < workingArea.Left)
                 { 
                     desiredLeft = workingArea.Left;
                 }
            }
            // If placed right, ensure it doesn't go off the bottom edge
            else if (desiredTop + windowHeight > workingArea.Bottom)
            {
                desiredTop = workingArea.Bottom - windowHeight;
                 // If placing right and clamped to bottom makes it go off top, clamp to top
                 if (desiredTop < workingArea.Top) { desiredTop = workingArea.Top; }
            }
             // Clamp top position if needed (can happen if initial targetRect.Top is near top)
             if (desiredTop < workingArea.Top) { desiredTop = workingArea.Top; } 


            // Set final window position
            this.Left = desiredLeft;
            this.Top = desiredTop;
        }
    }
} 