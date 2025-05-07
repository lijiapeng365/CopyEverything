using Microsoft.Extensions.Configuration;
using OmniGrab.Wpf.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace OmniGrab.Wpf.Views
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _currentSettings;
        private string _appSettingsPath;

        public SettingsWindow()
        {
            InitializeComponent();
            _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            _currentSettings = LoadSettings();
            DataContext = _currentSettings; // Set DataContext for potential binding, though direct access is used here.
            PopulateFields();
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_appSettingsPath))
                {
                    var json = File.ReadAllText(_appSettingsPath);
                    // Deserialize the entire root object, then get the Settings section
                    var rootObject = JsonSerializer.Deserialize<JsonElement>(json);
                    if (rootObject.TryGetProperty("Settings", out JsonElement settingsElement))
                    {
                        var settings = JsonSerializer.Deserialize<AppSettings>(settingsElement.GetRawText());
                        if (settings != null) return settings;
                    }
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                System.Windows.MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Return default settings if file doesn't exist or there's an error
            return new AppSettings 
            { 
                Ocr = new OcrSettings(), 
                Hotkey = new HotkeySettings { Key = "F1", Modifiers = "Control, Alt" },
                ResultWindow = new ResultWindowSettings() // Ensure this is initialized
            };
        }

        private void PopulateFields()
        {
            if (_currentSettings.Ocr != null)
            {
                ApiKeyTextBox.Text = _currentSettings.Ocr.ApiKey;
                EndpointUrlTextBox.Text = _currentSettings.Ocr.OcrEndpointUrl;
                ModelNameTextBox.Text = _currentSettings.Ocr.OcrModelName;
            }
            if (_currentSettings.Hotkey != null)
            {
                HotkeyKeyTextBox.Text = _currentSettings.Hotkey.Key;
                HotkeyModifiersTextBox.Text = _currentSettings.Hotkey.Modifiers;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Update _currentSettings from TextBoxes
                if (_currentSettings.Ocr == null) _currentSettings.Ocr = new OcrSettings();
                _currentSettings.Ocr.ApiKey = ApiKeyTextBox.Text;
                _currentSettings.Ocr.OcrEndpointUrl = EndpointUrlTextBox.Text;
                _currentSettings.Ocr.OcrModelName = ModelNameTextBox.Text;

                if (_currentSettings.Hotkey == null) _currentSettings.Hotkey = new HotkeySettings();
                _currentSettings.Hotkey.Key = HotkeyKeyTextBox.Text;
                _currentSettings.Hotkey.Modifiers = HotkeyModifiersTextBox.Text;

                // Construct the root object for serialization
                var rootObjectToSave = new { Settings = _currentSettings };
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(rootObjectToSave, options);
                File.WriteAllText(_appSettingsPath, json);

                System.Windows.MessageBox.Show("Settings saved successfully. Restart the application for changes to take full effect.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Indicates settings were saved
                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
} 