using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OmniGrab.Wpf.Services
{
    public class OcrServiceCaller
    {
        // Use a static HttpClient for performance reasons (avoids socket exhaustion)
        // See: https://docs.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly string _apiKey;
        private readonly string _ocrEndpointUrl;

        /// <summary>
        /// Initializes a new instance of the OcrServiceCaller.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="endpointUrl">The full URL of the OCR API endpoint.</param>
        public OcrServiceCaller(string apiKey, string endpointUrl)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey), "API Key cannot be empty.");
            if (string.IsNullOrWhiteSpace(endpointUrl))
                throw new ArgumentNullException(nameof(endpointUrl), "Endpoint URL cannot be empty.");
            if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Endpoint URL must be a valid absolute URI.", nameof(endpointUrl));

            _apiKey = apiKey;
            _ocrEndpointUrl = endpointUrl;

            // Set default timeout (optional, adjust as needed)
            // _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        /// <summary>
        /// Recognizes text from image data using an OpenAI-compatible vision API.
        /// </summary>
        /// <param name="imageData">Byte array of the image (e.g., PNG).</param>
        /// <param name="modelName">The model name to use (e.g., "gpt-4-vision-preview"). Defaults might vary.</param>
        /// <param name="prompt">The prompt instructing the model. Defaults to a simple extraction prompt.</param>
        /// <returns>The recognized text, or null on failure or if no text is found.</returns>
        public async Task<string?> RecognizeTextAsync(
            byte[] imageData,
            string modelName = "gpt-4-vision-preview", // Or allow configuration
            string prompt = "Extract all text content from this image.")
        {
            if (imageData == null || imageData.Length == 0)
            {
                Console.WriteLine("Error: Image data is empty.");
                return null;
            }

            string base64Image = Convert.ToBase64String(imageData);
            // Format as data URI for OpenAI API
            string dataUri = $"data:image/png;base64,{base64Image}";

            try
            {
                // Construct the request payload based on OpenAI Vision API
                var requestPayload = new JObject(
                    new JProperty("model", modelName),
                    new JProperty("messages", new JArray(
                        new JObject(
                            new JProperty("role", "user"),
                            new JProperty("content", new JArray(
                                new JObject(
                                    new JProperty("type", "text"),
                                    new JProperty("text", prompt)
                                ),
                                new JObject(
                                    new JProperty("type", "image_url"),
                                    new JProperty("image_url", new JObject(
                                        new JProperty("url", dataUri)
                                    ))
                                )
                            ))
                        )
                    )),
                    new JProperty("max_tokens", 1000) // Adjust max_tokens as needed
                );

                var requestContent = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");

                // Create a request message to set headers per-request
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _ocrEndpointUrl);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                requestMessage.Content = requestContent;

                // Send the request using the static HttpClient instance
                HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Parse the JSON response
                        var jsonResult = JObject.Parse(responseBody);

                        // Extract text from the expected location (OpenAI format)
                        string? recognizedText = jsonResult?["choices"]?[0]?["message"]?["content"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(recognizedText))
                        {
                             Console.WriteLine("OCR Successful.");
                             return recognizedText.Trim();
                        }
                        else
                        {
                            Console.WriteLine($"OCR successful, but no text content found in response. Response: {responseBody}");
                            return string.Empty; // Indicate success but no text
                        }
                    }
                    catch (Newtonsoft.Json.JsonException jsonEx)
                    {
                        Console.WriteLine($"Error parsing OCR JSON response: {jsonEx.Message}. Response Body: {responseBody}");
                        return null; // Indicate failure due to parsing error
                    }
                }
                else
                {
                    Console.WriteLine($"OCR API request failed. Status: {response.StatusCode}. Response: {responseBody}");
                    // Consider parsing the error response body for a more specific message
                    // Example: Try parsing as { "error": { "message": "..." } }
                    string errorMessage = responseBody;
                    try
                    {
                        var errorJson = JObject.Parse(responseBody);
                        errorMessage = errorJson?["error"]?["message"]?.ToString() ?? errorMessage;
                    }
                    catch { /* Ignore parsing error, use raw body */ }
                    // Throw an exception might be better for clearer error handling upstream
                    throw new HttpRequestException($"OCR API error: {response.StatusCode} - {errorMessage}");
                    // return null; // Or return null to indicate failure
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"Network error calling OCR API: {httpEx.Message}");
                throw; // Re-throw the exception for upstream handling
                // return null;
            }
            catch (TaskCanceledException timeoutEx) // Catches HttpClient timeouts
            {
                 Console.WriteLine($"OCR API request timed out: {timeoutEx.Message}");
                 throw; // Re-throw
                 // return null;
            }
            catch (Exception ex) // Catch-all for unexpected errors during the process
            {
                Console.WriteLine($"An unexpected error occurred during OCR processing: {ex.ToString()}");
                throw; // Re-throw
                // return null;
            }
        }
    }
} 