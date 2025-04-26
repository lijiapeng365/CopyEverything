using System;
using System.Drawing;          // For Bitmap, Graphics, Rectangle - Requires System.Drawing.Common NuGet package
using System.Drawing.Imaging;    // For ImageFormat
using System.IO;                 // For MemoryStream
using System.Windows;          // For System.Windows.Rect used as input

namespace CopyEverythingOcr.Wpf.Services
{
    public static class ScreenCaptureService // Making it static as it doesn't hold state
    {
        /// <summary>
        /// Captures a specific rectangle of the screen.
        /// Assumes the input Rect is in device-independent units (WPF units) corresponding to screen coordinates.
        /// Requires the application to be DPI aware.
        /// </summary>
        /// <param name="region">The rectangle to capture, in screen coordinates (WPF units).</param>
        /// <returns>Byte array containing the PNG image data, or null on error.</returns>
        public static byte[] CaptureScreen(Rect region)
        {
            // Convert WPF Rect (double) to System.Drawing.Rectangle (int)
            // This involves casting and might need adjustments based on DPI, but SelectionOverlayWindow already calculated DPI-aware coordinates.
            int x = (int)Math.Round(region.X);
            int y = (int)Math.Round(region.Y);
            int width = (int)Math.Round(region.Width);
            int height = (int)Math.Round(region.Height);

            // Basic validation
            if (width <= 0 || height <= 0)
            {
                Console.WriteLine("Error: Invalid rectangle dimensions for screen capture.");
                return null;
            }

            // Create a System.Drawing.Rectangle
            Rectangle captureRect = new Rectangle(x, y, width, height);

            try
            {
                // Ensure rectangle is within virtual screen bounds (optional but good practice)
                // Rectangle virtualScreen = System.Windows.Forms.SystemInformation.VirtualScreen; // Requires WinForms reference
                // captureRect.Intersect(virtualScreen);
                // if (captureRect.Width <= 0 || captureRect.Height <= 0) return null;

                using (Bitmap bmp = new Bitmap(captureRect.Width, captureRect.Height, PixelFormat.Format32bppArgb))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // Copy from screen using the specified rectangle coordinates
                    // The coordinates are screen coordinates.
                    g.CopyFromScreen(captureRect.X, captureRect.Y, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png); // Save as PNG
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screen: {ex.Message}");
                // Consider logging the full exception (ex.ToString())
                return null;
            }
        }
    }
} 