using System.Windows;
using CopyEverythingOcr.Wpf.Services;
using CopyEverythingOcr.Wpf.Views;
using CopyEverythingOcr.Wpf.Models;
using System.Windows.Forms;
using System.ComponentModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;

namespace CopyEverythingOcr.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private HotkeyManager? _hotkeyManager;
    private MainWindow? _mainWindow;

    private bool _isCapturing = false;

    private AppSettings? _appSettings;

    private ResultWindow? _currentResultWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LoadConfiguration();

        _mainWindow = new MainWindow();

        // Subscribe to the Loaded event to initialize hotkeys after window is ready
        _mainWindow.Loaded += MainWindow_Loaded;

        // Show the main window
        _mainWindow.Show();

        // Hotkey initialization moved to MainWindow_Loaded
    }

    private void LoadConfiguration()
    {
        try
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            _appSettings = configuration.GetSection("Settings").Get<AppSettings>();

            if (_appSettings == null)
            {
                System.Windows.MessageBox.Show("Failed to load settings from appsettings.json. Please ensure the file exists and is correctly formatted.",
                                           "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _appSettings = new AppSettings();
            }
            _appSettings.Ocr ??= new OcrSettings();
            _appSettings.Hotkey ??= new HotkeySettings { Key = "F1", Modifiers = "Control, Alt" };
            _appSettings.ResultWindow ??= new ResultWindowSettings();
        }
        catch (FileNotFoundException)
        {
            System.Windows.MessageBox.Show("appsettings.json not found. Using default settings.",
                                       "Configuration Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            _appSettings = new AppSettings { Ocr = new OcrSettings(), Hotkey = new HotkeySettings { Key = "F1", Modifiers = "Control, Alt" }, ResultWindow = new ResultWindowSettings() };
        }
        catch (JsonException ex)
        {
            System.Windows.MessageBox.Show($"Error reading appsettings.json: {ex.Message}. Using default settings.",
                                       "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _appSettings = new AppSettings { Ocr = new OcrSettings(), Hotkey = new HotkeySettings { Key = "F1", Modifiers = "Control, Alt" }, ResultWindow = new ResultWindowSettings() };
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"An unexpected error occurred while loading configuration: {ex.Message}. Using default settings.",
                                       "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _appSettings = new AppSettings { Ocr = new OcrSettings(), Hotkey = new HotkeySettings { Key = "F1", Modifiers = "Control, Alt" }, ResultWindow = new ResultWindowSettings() };
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mainWindow == null || _appSettings == null || _appSettings.Hotkey == null) return;
        _mainWindow.Loaded -= MainWindow_Loaded;

        try
        {
            Keys key = Keys.F1;
            ModifierKeys modifiers = ModifierKeys.Control | ModifierKeys.Alt;

            if (!string.IsNullOrWhiteSpace(_appSettings.Hotkey.Key) &&
                Enum.TryParse<Keys>(_appSettings.Hotkey.Key, true, out var parsedKey))
            {
                key = parsedKey;
            }
            else
            {
                Debug.WriteLine($"Warning: Could not parse Hotkey Key '{_appSettings.Hotkey.Key}'. Using default.");
            }

            if (!string.IsNullOrWhiteSpace(_appSettings.Hotkey.Modifiers))
            {
                modifiers = ParseModifierKeys(_appSettings.Hotkey.Modifiers);
            }

            _hotkeyManager = new HotkeyManager(_mainWindow);
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

    private ModifierKeys ParseModifierKeys(string modifiersString)
    {
        ModifierKeys result = ModifierKeys.None;
        if (string.IsNullOrWhiteSpace(modifiersString)) return result;

        var parts = modifiersString.Split(new[] { ',', ' ', '+' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (Enum.TryParse<ModifierKeys>(part, true, out var parsedModifier))
            {
                result |= parsedModifier;
            }
            else
            {
                Debug.WriteLine($"Warning: Could not parse modifier key '{part}'.");
            }
        }
        return result;
    }

    private async void HotkeyManager_HotkeyPressed(object? sender, EventArgs e)
    {
        if (_isCapturing || _appSettings == null || _appSettings.Ocr == null)
        {
            Debug.WriteLine("Capture already in progress or settings not loaded.");
            return;
        }

        _isCapturing = true;
        Debug.WriteLine("Hotkey Pressed! Starting screen capture process...");

        Rect selectedRegion = Rect.Empty;

        try
        {
            var overlay = new SelectionOverlayWindow();
            bool? dialogResult = overlay.ShowDialog();

            if (dialogResult == true)
            {
                selectedRegion = overlay.SelectedRegion;
                if (!selectedRegion.IsEmpty)
                {
                    Debug.WriteLine($"Region selected: {selectedRegion}");

                    byte[]? screenshotData = ScreenCaptureService.CaptureScreen(selectedRegion);

                    if (screenshotData != null)
                    {
                        Debug.WriteLine($"Screenshot captured successfully ({screenshotData.Length} bytes).");

                        string? apiKey = _appSettings.Ocr.ApiKey;
                        string? endpointUrl = _appSettings.Ocr.OcrEndpointUrl;
                        string? modelName = _appSettings.Ocr.OcrModelName;

                        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(endpointUrl))
                        {
                            System.Windows.MessageBox.Show("API Key or Endpoint URL is not configured in appsettings.json!",
                                                       "Configuration Needed", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        modelName = !string.IsNullOrWhiteSpace(modelName) ? modelName : "gpt-4-vision-preview";

                        var ocrCaller = new OcrServiceCaller(apiKey, endpointUrl);
                        Debug.WriteLine($"Calling OCR Service (Model: {modelName})...");

                        string? recognizedText = null;
                        try
                        {
                            recognizedText = await ocrCaller.RecognizeTextAsync(screenshotData, modelName: modelName);
                        }
                        catch (HttpRequestException httpEx)
                        {
                            Debug.WriteLine($"OCR API Call Failed: {httpEx.Message}");
                            System.Windows.MessageBox.Show($"Failed to connect to OCR service: {httpEx.Message}", "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"OCR Processing Failed: {ex.ToString()}");
                            System.Windows.MessageBox.Show($"An error occurred during OCR processing: {ex.Message}", "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        if (recognizedText != null)
                        {
                            Debug.WriteLine($"OCR Result: {recognizedText}");

                            _currentResultWindow = new ResultWindow();
                            _currentResultWindow.ResultText = recognizedText;

                            _currentResultWindow.PositionNear(selectedRegion);
                            _currentResultWindow.Show();
                        }
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
        }
        finally
        {
            _isCapturing = false;
            Debug.WriteLine("Capture process finished.");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        Debug.WriteLine("Hotkey unregistered on exit.");

        base.OnExit(e);
    }
}

