namespace OmniGrab.Wpf.Models
{
    // Represents the top-level structure in appsettings.json
    public class AppSettings
    {
        public OcrSettings? Ocr { get; set; }
        public HotkeySettings? Hotkey { get; set; } 
        public ResultWindowSettings? ResultWindow { get; set; }
    }
} 