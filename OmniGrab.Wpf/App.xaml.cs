using System.Windows;
using OmniGrab.Wpf.Services;
using OmniGrab.Wpf.Views;
using OmniGrab.Wpf.Models;
using System.Windows.Forms;
using System.ComponentModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using System.Drawing;
using System.Globalization;

namespace OmniGrab.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private HotkeyManager? _hotkeyManager;
    private MainWindow? _mainWindow;
    private NotifyIcon? _notifyIcon;
    private SettingsWindow? _settingsWindow;

    private bool _isCapturing = false;

    private AppSettings? _appSettings;

    private ResultWindow? _currentResultWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LoadConfiguration();

        _mainWindow = new MainWindow();
        _mainWindow.ShowInTaskbar = false;
        _mainWindow.Visibility = Visibility.Collapsed;

        if (_appSettings == null || _appSettings.Hotkey == null)
        {
            Debug.WriteLine(StringResources.ErrorCriticalInitGeneric);
            Shutdown(-1);
            return;
        }

        InitializeNotifyIcon();

        if (_mainWindow != null)
        {
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
                    Debug.WriteLine(string.Format(StringResources.WarningParseKeyFailed, _appSettings.Hotkey.Key, "F1"));
                }

                if (!string.IsNullOrWhiteSpace(_appSettings.Hotkey.Modifiers))
                {
                    modifiers = ParseModifierKeys(_appSettings.Hotkey.Modifiers);
                    if (modifiers == ModifierKeys.None && !string.IsNullOrWhiteSpace(_appSettings.Hotkey.Modifiers))
                    {
                        Debug.WriteLine(string.Format(StringResources.WarningParseModifiersFailed, _appSettings.Hotkey.Modifiers, "Control+Alt"));
                        modifiers = ModifierKeys.Control | ModifierKeys.Alt;
                    }
                }
                else
                {
                    Debug.WriteLine(string.Format(StringResources.WarningModifiersNotSpecified, "Control+Alt"));
                }

                _hotkeyManager = new HotkeyManager(_mainWindow);
                _hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;
                
                if (modifiers != ModifierKeys.None || key != Keys.None)
                {
                    _hotkeyManager.Register(key, modifiers);
                    Debug.WriteLine(string.Format(StringResources.InfoHotKeyRegistered, modifiers, key));
                }
                else
                {
                    Debug.WriteLine(StringResources.WarningHotkeyNotRegistered);
                }
            }
            catch (Win32Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(StringResources.ErrorHotkeyRegisterFailed, ex.Message),
                                StringResources.ErrorHotkeyTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (InvalidOperationException ex)
            {
                System.Windows.MessageBox.Show(string.Format(StringResources.ErrorInitHotkeyManagerFailed, ex.Message), 
                                StringResources.ErrorInitTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(StringResources.ErrorUnexpectedHotkeySetup, ex.Message), 
                                StringResources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Hotkey Setup Exception: {ex}");
            }
        }
        else
        {
            Debug.WriteLine(StringResources.ErrorCriticalInitGeneric);
            System.Windows.MessageBox.Show(StringResources.ErrorCriticalInitGeneric, 
                            StringResources.ErrorCriticalInitGeneric, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        Debug.WriteLine(StringResources.AppTitle + " started, running in system tray.");
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
                System.Windows.MessageBox.Show(StringResources.ErrorMsgLoadConfigFailed,
                                           StringResources.ErrorConfigTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                _appSettings = new AppSettings();
            }
            _appSettings.Ocr ??= new OcrSettings();
            _appSettings.Hotkey ??= new HotkeySettings { Key = "F1", Modifiers = "Control, Alt" };
            _appSettings.ResultWindow ??= new ResultWindowSettings();
        }
        catch (FileNotFoundException)
        {
            System.Windows.MessageBox.Show(StringResources.WarningAppsettingsNotFound,
                                       StringResources.WarningConfigTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            _appSettings = new AppSettings { Ocr = new OcrSettings(), Hotkey = new HotkeySettings { Key = "F1", Modifiers = "Control, Alt" }, ResultWindow = new ResultWindowSettings() };
        }
        catch (System.Text.Json.JsonException ex)
        {
            System.Windows.MessageBox.Show(string.Format(StringResources.ErrorReadingAppsettings, ex.Message),
                                       StringResources.ErrorConfigTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            _appSettings = new AppSettings { Ocr = new OcrSettings(), Hotkey = new HotkeySettings { Key = "F1", Modifiers = "Control, Alt" }, ResultWindow = new ResultWindowSettings() };
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(string.Format(StringResources.ErrorUnexpectedLoadConfig, ex.Message),
                                       StringResources.ErrorConfigTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            _appSettings = new AppSettings { Ocr = new OcrSettings(), Hotkey = new HotkeySettings { Key = "F1", Modifiers = "Control, Alt" }, ResultWindow = new ResultWindowSettings() };
        }
    }

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = GetAppIcon();
        _notifyIcon.Text = StringResources.AppTitle;
        _notifyIcon.Visible = true;

        var contextMenu = new ContextMenuStrip();
        var settingsMenuItem = new ToolStripMenuItem(StringResources.MenuSettings);
        settingsMenuItem.Click += SettingsMenuItem_Click;
        var exitMenuItem = new ToolStripMenuItem(StringResources.MenuExit);
        exitMenuItem.Click += ExitMenuItem_Click;

        contextMenu.Items.Add(settingsMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

         _notifyIcon.DoubleClick += SettingsMenuItem_Click;

        Debug.WriteLine("NotifyIcon initialized.");
    }

    private Icon GetAppIcon()
    {
        try
        {
             string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
             if (File.Exists(iconPath))
             {
                 return new Icon(iconPath);
             }
            Debug.WriteLine($"Warning: Application icon not found at '{iconPath}'. Using default system icon.");
            return SystemIcons.Application;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading icon: {ex.Message}");
            return SystemIcons.Application;
        }
    }

    private void SettingsMenuItem_Click(object? sender, EventArgs e)
    {
        ShowSettingsWindow();
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        Shutdown();
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (s, args) => _settingsWindow = null;
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }
        else
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }
            _settingsWindow.Activate();
        }
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
                            System.Windows.MessageBox.Show(StringResources.MsgConfigNeeded,
                                                       StringResources.TitleConfigNeeded, MessageBoxButton.OK, MessageBoxImage.Warning);
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
                            System.Windows.MessageBox.Show(string.Format(StringResources.ErrorOcrConnectFailed, httpEx.Message), 
                                            StringResources.TitleOcrError, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"OCR Processing Failed: {ex.ToString()}");
                            System.Windows.MessageBox.Show(string.Format(StringResources.ErrorOcrProcessing, ex.Message), 
                                            StringResources.TitleOcrError, MessageBoxButton.OK, MessageBoxImage.Error);
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
                        System.Windows.MessageBox.Show(StringResources.ErrorCaptureFailed, 
                                        StringResources.TitleCaptureError, MessageBoxButton.OK, MessageBoxImage.Warning);
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
        _notifyIcon?.Dispose();
        _hotkeyManager?.Dispose();
        Debug.WriteLine("Hotkey unregistered and NotifyIcon disposed on exit.");

        base.OnExit(e);
    }
}

