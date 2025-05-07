namespace OmniGrab.Wpf.Models;

public class OcrSettings
{
    public string? ApiKey { get; set; }
    public string? SecretKey { get; set; } // Optional, depending on service
    public string? OcrEndpointUrl { get; set; }
    public string? OcrModelName { get; set; } // Added for model selection
} 