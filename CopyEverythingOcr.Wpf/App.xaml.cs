using System.Windows;
using CopyEverythingOcr.Wpf.Services;
using CopyEverythingOcr.Wpf.Views;
using System.Windows.Forms;
using System.ComponentModel;
using System;
using System.Diagnostics;

namespace CopyEverythingOcr.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private HotkeyManager? _hotkeyManager;
    private MainWindow? _mainWindow;

    private bool _isCapturing = false;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow();

        // Subscribe to the Loaded event to initialize hotkeys after window is ready
        _mainWindow.Loaded += MainWindow_Loaded;

        // Show the main window
        _mainWindow.Show();

        // Hotkey initialization moved to MainWindow_Loaded
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Ensure mainWindow is not null before proceeding
        if (_mainWindow == null) return;

        // Unsubscribe from the event to prevent repeated execution if Loaded fires again
        _mainWindow.Loaded -= MainWindow_Loaded;

        // Now initialize the HotkeyManager
        try
        {
            _hotkeyManager = new HotkeyManager(_mainWindow);

            // TODO: Read hotkey configuration from appsettings.json
            Keys key = Keys.F1;
            ModifierKeys modifiers = ModifierKeys.Control | ModifierKeys.Alt;

            _hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;
            _hotkeyManager.Register(key, modifiers);

            Debug.WriteLine($"Hotkey registered after MainWindow loaded: {modifiers} + {key}");
        }
        catch (Win32Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to register hotkey: {ex.Message}\n" +
                            "Please check if another application is using the same hotkey or change it in settings.",
                            "Hotkey Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (InvalidOperationException ex)
        {
            System.Windows.MessageBox.Show($"Failed to initialize hotkey manager: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"An unexpected error occurred during hotkey setup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // TODO: Implement System Tray Icon logic here (might also need window handle)
    }

    private void HotkeyManager_HotkeyPressed(object? sender, EventArgs e)
    {
        if (_isCapturing)
        {
            Debug.WriteLine("Capture already in progress. Ignoring hotkey press.");
            return;
        }

        _isCapturing = true;
        Debug.WriteLine("Hotkey Pressed! Starting screen capture process...");

        var overlay = new SelectionOverlayWindow();
        bool? dialogResult = overlay.ShowDialog();

        if (dialogResult == true)
        {
            Rect selectedRegion = overlay.SelectedRegion;
            if (!selectedRegion.IsEmpty)
            {
                Debug.WriteLine($"Region selected: {selectedRegion}");

                byte[]? screenshotData = ScreenCaptureService.CaptureScreen(selectedRegion);

                if (screenshotData != null)
                {
                    Debug.WriteLine($"Screenshot captured successfully ({screenshotData.Length} bytes).");
                    System.Windows.MessageBox.Show("Screenshot Captured! Now call OCR.");
                }
                else
                {
                    Debug.WriteLine("Screenshot capture failed.");
                    System.Windows.MessageBox.Show("Failed to capture the screen region.", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                Debug.WriteLine("Selection resulted in an empty region.");
            }
        }
        else
        {
            Debug.WriteLine("Screen capture cancelled by user (ESC or no selection).");
        }

        _isCapturing = false;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        Debug.WriteLine("Hotkey unregistered on exit.");

        base.OnExit(e);
    }
}

