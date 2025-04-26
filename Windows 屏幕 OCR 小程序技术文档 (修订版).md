**Windows 屏幕 OCR 小程序技术文档 (修订版)**

**一、项目概述**

本项目旨在开发一个 Windows 小程序，该程序能够在系统后台运行，用户按下可配置的快捷键后可框选电脑屏幕上的内容，调用接入的 OCR 模型（如 OpenAI、百度 AI 等）识别图片中的字符，并将识别结果展示在一个置顶的、可拖动的小窗口中，方便用户复制。程序需注重用户体验、稳定性和安全性。

**二、技术选型**

1.  **编程语言：C#**
    *   **理由：** 微软主力语言，与 Windows API 集成最佳，生态成熟，开发效率与性能兼顾。

2.  **框架：.NET (推荐 .NET 6/7/8 或更高版本)**
    *   **理由：** 跨平台基础（虽然本项目仅 Windows），性能优越，长期支持，现代化。支持自包含部署，简化用户端依赖。

3.  **UI 框架：WinForms 或 WPF**
    *   **WinForms：**
        *   优点：上手快，适合快速开发简单界面（如结果展示窗口）。
        *   缺点：界面风格较旧，自定义绘制（尤其是半透明、无闪烁的屏幕选框覆盖层）相对复杂。
    *   **WPF：**
        *   优点：界面现代，强大的样式和数据绑定，对图形绘制、动画、透明窗口支持极佳，非常适合实现流畅的屏幕选框覆盖层。
        *   缺点：学习曲线稍陡。
    *   **选择建议：**
        *   **混合方案 (推荐):** 使用 WPF 创建屏幕选框的覆盖窗口（利用其图形和透明优势），使用 WinForms 或 WPF 创建结果展示窗口（根据偏好选择）。
        *   **纯 WPF:** 如果熟悉 WPF 或希望界面更统一、现代。
        *   **纯 WinForms:** 如果追求最快开发速度且能接受选框实现的挑战（可能需要 GDI+ 绘图和双缓冲技术避免闪烁）。

**三、功能模块实现**

1.  **后台运行/系统托盘**
    *   **实现：**
        *   WinForms: 使用 `NotifyIcon` 控件。
        *   WPF: 使用第三方库 (如 `Hardcodet.NotifyIcon.Wpf`) 或 P/Invoke 实现。
    *   **行为：** 程序启动后主界面不显示，仅在系统托盘区显示图标。提供右键菜单（如：设置、关于、退出）。
    *   **配置：** (可选) 提供开机自启动选项。

2.  **全局快捷键监听**
    *   **实现：** 使用 P/Invoke 调用 Windows API `RegisterHotKey` / `UnregisterHotKey`。在隐藏窗口或消息循环中处理 `WM_HOTKEY` 消息。
    *   **健壮性：**
        *   处理注册失败的情况（如快捷键已被占用）。
        *   程序退出时务必调用 `UnregisterHotKey` 释放快捷键。
    *   **配置：** (推荐) 允许用户在设置界面自定义触发截图的快捷键。

3.  **屏幕框选与截图**
    *   **框选实现：**
        *   快捷键触发后，创建一个（或多个，覆盖所有显示器）**全屏、置顶、半透明**的覆盖窗口 (WPF `Window` 或 WinForms `Form`，推荐 WPF)。
        *   在该窗口上监听鼠标事件：`MouseDown` (记录起点，开始绘制)，`MouseMove` (实时绘制选框矩形)，`MouseUp` (记录终点，确定最终选区)。
        *   绘制选框时，可以反色或使用醒目的边框，并显示当前选区尺寸。
        *   支持按 `ESC` 键取消框选。
    *   **多显示器处理：** 正确获取所有屏幕的边界，确保覆盖窗口能覆盖所有屏幕，坐标计算需要考虑虚拟屏幕坐标系。
    *   **DPI 感知：** 程序需要声明为 DPI 感知，以在不同缩放设置的显示器上正确获取坐标和截图。使用 `GetDeviceCaps` API 或 .NET 提供的相关方法获取 DPI 信息。
    *   **截图实现：**
        *   根据用户框选的最终矩形坐标（转换为屏幕绝对坐标），使用 `System.Drawing.Graphics.CopyFromScreen` (需要 `System.Drawing.Common` NuGet 包) 从屏幕缓冲区抓取图像。
        *   将 `Bitmap` 对象保存到内存流 (`MemoryStream`) 中，通常使用 `Png` 或 `Jpeg` 格式。

4.  **调用 OCR 模型 API**
    *   **实现：** 使用 `System.Net.Http.HttpClient` 发送异步 HTTP 请求。
    *   **安全性：**
        *   **严禁硬编码 API Key/Secret Key！**
        *   应将凭证存储在安全的外部位置，如：
            *   用户配置文件（加密存储）。
            *   Windows 凭据管理器。
            *   环境变量（安全性较低）。
        *   程序启动时读取凭证。提供设置界面让用户输入和保存自己的 Key。
    *   **请求构建：**
        *   根据所选 OCR 服务文档，构建请求。通常需要：
            *   将截图的 `byte[]` 进行 Base64 编码。
            *   设置请求头（如 `Content-Type`, `Authorization` 或包含 `access_token` 的 URL 参数）。
            *   将 Base64 图片数据和其它参数（如语言）放入请求体（通常是 JSON 或 `x-www-form-urlencoded`）。
    *   **响应处理与错误处理：**
        *   使用 `try-catch` 块捕获网络异常（`HttpRequestException` 等）、超时 (`TaskCanceledException`)。
        *   检查 HTTP 响应状态码 (`HttpResponseMessage.IsSuccessStatusCode`)。非 2xx 状态码表示请求失败，需要解析错误信息（通常在响应体中）并向用户提示。
        *   处理 API 特定的错误码（如额度用尽、认证失败、图片格式不支持、识别失败等）。
        *   使用 `System.Text.Json` 或 `Newtonsoft.Json` 解析成功的 JSON 响应，提取识别出的文本。
        *   考虑 API 的调用频率限制，避免短时间内过多请求。
    *   **异步处理：** 整个截图到识别的过程应该是异步的，避免阻塞 UI 线程，并显示适当的“识别中...”状态提示。

5.  **结果展示**
    *   **实现：** 创建一个简单的、无边框、置顶的小窗口 (`Form` 或 `Window`)。
    *   **内容：**
        *   使用只读 `TextBox` (WinForms) 或 `TextBlock`/`TextBox` (WPF) 显示识别出的文本。支持多行显示。
        *   保留原始文本格式（换行、段落）比简单 `string.Join` 更好，需要根据 API 返回的结构（如文字块、行信息）来重建。
        *   添加一个“复制”按钮，点击后将文本复制到剪贴板 (`System.Windows.Clipboard.SetText()`)。
        *   (可选) 提供“编辑”功能，允许用户在复制前修改识别结果。
    *   **行为与交互：**
        *   **位置：** 窗口默认显示在截图区域的右侧或下方，避免遮挡原文。
        *   **拖动：** 支持用户通过鼠标按住窗口空白区域（或特定标题栏区域）拖动窗口。
        *   **关闭：** 提供明确的关闭方式：小的关闭按钮 (`X`)、按 `ESC` 键、点击窗口外部区域。
        *   **无结果处理：** 若 OCR 未识别到文字或出错，窗口应显示明确提示（如“未识别到文字”或具体的错误信息）。
        *   **焦点：** 窗口弹出后可以考虑不抢占当前焦点，以免打断用户流程，但提供方便的复制方式。
    *   **配置：** (可选) 允许用户设置结果窗口的默认字体大小、背景色、是否自动复制等。

**四、代码示例 (增强版，增加注释和健壮性考虑)**

1.  **全局快捷键监听示例（使用 P/Invoke）**
    ```csharp
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms; // For Keys enum and Message
    using System.ComponentModel; // For Win32Exception

    // Represents the modifier keys for a hotkey
    [Flags]
    public enum ModifierKeys : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }

    public class HotkeyManager : NativeWindow, IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409; // Win32 Error Code

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly int _hotkeyId; // Use a stable ID, e.g., based on a counter or specific value
        private bool _isRegistered = false;
        private bool _disposed = false;

        public event EventHandler HotkeyPressed;

        // Use a unique ID for each hotkey instance if managing multiple
        public HotkeyManager(int id = 0) // Default ID 0, manage IDs if needed
        {
            _hotkeyId = id == 0 ? this.GetHashCode() : id; // Basic ID generation
            // Create a message-only window handle
            CreateHandle(new CreateParams() { Parent = IntPtr.Zero });
        }

        /// <summary>
        /// Registers the hotkey.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="modifiers">The modifier keys.</param>
        /// <exception cref="Win32Exception">Thrown if registration fails.</exception>
        public void Register(Keys key, ModifierKeys modifiers)
        {
            if (_isRegistered)
            {
                Unregister(); // Unregister previous if any
            }

            if (!RegisterHotKey(Handle, _hotkeyId, (uint)modifiers, (uint)key))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == ERROR_HOTKEY_ALREADY_REGISTERED)
                {
                     throw new Win32Exception(errorCode, $"Hotkey (ID: {_hotkeyId}, Key: {key}, Modifiers: {modifiers}) is already registered by another application.");
                }
                else
                {
                     throw new Win32Exception(errorCode, $"Failed to register hotkey (ID: {_hotkeyId}). Win32 Error: {errorCode}");
                }
            }
            _isRegistered = true;
        }

        /// <summary>
        /// Unregisters the hotkey.
        /// </summary>
        public void Unregister()
        {
            if (_isRegistered)
            {
                if (!UnregisterHotKey(Handle, _hotkeyId))
                {
                    // Log error, but don't throw usually, as it might happen during shutdown
                    int errorCode = Marshal.GetLastWin32Error();
                    Console.WriteLine($"Warning: Failed to unregister hotkey (ID: {_hotkeyId}). Win32 Error: {errorCode}");
                }
                _isRegistered = false;
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY)
            {
                if (m.WParam.ToInt32() == _hotkeyId)
                {
                    // Raise the event on the UI thread if necessary
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    // No managed objects to dispose in this simple example.
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                Unregister(); // Ensure hotkey is unregistered
                if (Handle != IntPtr.Zero)
                {
                    DestroyHandle();
                }

                _disposed = true;
            }
        }

         ~HotkeyManager()
         {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
         }
    }
    ```

2.  **屏幕截图示例 (基本不变，增加 using)**
    ```csharp
    using System;
    using System.Drawing; // For Bitmap, Graphics, Rectangle
    using System.Drawing.Imaging; // For ImageFormat
    using System.IO; // For MemoryStream
    using System.Windows.Forms; // For Screen class (optional, helps with multi-monitor bounds)

    public class ScreenCapture
    {
        /// <summary>
        /// Captures a specific rectangle of the screen.
        /// Ensure the application is DPI aware for accurate coordinates.
        /// </summary>
        /// <param name="rect">The rectangle to capture, in screen coordinates.</param>
        /// <returns>Byte array containing the PNG image data, or null on error.</returns>
        public static byte[] CaptureScreen(Rectangle rect)
        {
            // Basic validation
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                Console.WriteLine("Error: Invalid rectangle dimensions for screen capture.");
                return null;
            }

            try
            {
                // Ensure rectangle is within screen bounds (optional but good practice)
                // Rectangle screenBounds = SystemInformation.VirtualScreen; // Gets combined bounds of all screens
                // rect.Intersect(screenBounds);
                // if (rect.Width <= 0 || rect.Height <= 0) return null;

                using (Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb)) // Use a common pixel format
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // Copy from screen using the specified rectangle coordinates
                    g.CopyFromScreen(rect.X, rect.Y, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png); // Save as PNG (lossless, supports transparency)
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screen: {ex.Message}");
                // Log the exception details (ex.ToString())
                return null;
            }
        }
    }
    ```

3.  **调用 OCR 服务示例（以通用方式，强调安全和错误处理）**
    ```csharp
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers; // For AuthenticationHeaderValue
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json; // Requires Newtonsoft.Json NuGet package
    using Newtonsoft.Json.Linq; // For flexible JSON parsing

    public class OcrServiceCaller
    {
        // IMPORTANT: NEVER hardcode credentials! Load from secure storage.
        private readonly string _apiKey;
        private readonly string _secretKey; // Or Access Token, depending on the service
        private readonly string _ocrEndpointUrl; // e.g., "https://api.ocr.service.com/v1/recognize"
        private readonly HttpClient _httpClient;

        // Constructor accepting credentials and endpoint
        public OcrServiceCaller(string apiKey, string secretKey, string endpointUrl)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(endpointUrl))
            {
                throw new ArgumentException("API Key and Endpoint URL cannot be empty.");
            }
             // _secretKey might be optional depending on auth method

            _apiKey = apiKey;
            _secretKey = secretKey; // Store securely if needed for token generation
            _ocrEndpointUrl = endpointUrl;

            _httpClient = new HttpClient();
            // Set default timeout (adjust as needed)
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            // Add common headers if needed (e.g., User-Agent)
            // _httpClient.DefaultRequestHeaders.Add("User-Agent", "MyOcrApp/1.0");
        }

        /// <summary>
        /// Recognizes text from image data using the configured OCR service.
        /// </summary>
        /// <param name="imageData">Byte array of the image (e.g., PNG).</param>
        /// <returns>The recognized text, or null/empty string on failure.</returns>
        public async Task<string> RecognizeTextAsync(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                Console.WriteLine("Error: Image data is empty.");
                return null;
            }

            string base64Image = Convert.ToBase64String(imageData);

            try
            {
                // --- Prepare Request (Example using JSON body, adapt as needed) ---
                var requestPayload = new JObject( // Using JObject for flexibility
                    new JProperty("image", base64Image),
                    new JProperty("language", "auto") // Example parameter, adjust for specific API
                    // Add other parameters like language, options, etc.
                );
                var content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");

                // --- Add Authentication (Example: Bearer Token using API Key) ---
                // Adapt this based on the specific OCR service's requirements
                // Option 1: API Key in Header
                // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", _apiKey);
                // Option 2: Bearer Token (if API key is the token)
                 _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                // Option 3: Custom Header
                // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", _apiKey);
                // Option 4: Access Token in URL (like Baidu example, construct URL dynamically)
                // string urlWithToken = $"{_ocrEndpointUrl}?access_token={await GetAccessTokenIfNeeded()}";

                // --- Send Request ---
                HttpResponseMessage response = await _httpClient.PostAsync(_ocrEndpointUrl, content); // Or GetAsync if needed

                // --- Handle Response ---
                if (response.IsSuccessStatusCode) // Check for 2xx status codes
                {
                    string jsonResult = await response.Content.ReadAsStringAsync();

                    // --- Parse Result (Highly dependent on API response structure) ---
                    try
                    {
                        // Example: Assuming result has a structure like { "texts": [ {"text": "line1"}, {"text": "line2"} ] }
                        var parsedJson = JObject.Parse(jsonResult);
                        var textItems = parsedJson["texts"] as JArray; // Adjust path "texts"

                        if (textItems != null && textItems.Count > 0)
                        {
                            var sb = new StringBuilder();
                            foreach (var item in textItems)
                            {
                                sb.AppendLine(item["text"]?.ToString() ?? ""); // Adjust property name "text"
                            }
                            return sb.ToString().Trim();
                        }
                        else
                        {
                            // Handle cases where API returns success but no text found
                            Console.WriteLine("OCR successful, but no text recognized.");
                            return string.Empty; // Or a specific message
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                         Console.WriteLine($"Error parsing OCR JSON response: {jsonEx.Message}");
                         // Log full jsonResult for debugging
                         return null; // Indicate failure
                    }
                }
                else
                {
                    // Handle API error response (4xx, 5xx status codes)
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"OCR API request failed. Status: {response.StatusCode}. Response: {errorContent}");
                    // TODO: Parse errorContent for specific error message if available
                    // Consider mapping status codes (401=Unauthorized, 429=RateLimit, etc.) to specific user feedback
                    return null; // Indicate failure
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"Network error calling OCR API: {httpEx.Message}");
                // Handle network issues (DNS, connection refused, etc.)
                return null;
            }
            catch (TaskCanceledException timeoutEx)
            {
                 Console.WriteLine($"OCR API request timed out: {timeoutEx.Message}");
                 return null;
            }
            catch (Exception ex) // Catch-all for unexpected errors
            {
                Console.WriteLine($"An unexpected error occurred during OCR processing: {ex.Message}");
                // Log ex.ToString() for full details
                return null;
            }
        }

        // Example: Helper if access token needs separate generation (like Baidu)
        // private async Task<string> GetAccessTokenIfNeeded() { /* ... Implementation ... */ }
    }
    ```

**五、部署与测试**

1.  **部署**
    *   **依赖：** 确认目标 Windows 版本，确保安装了对应版本的 .NET 运行时（如果采用框架依赖部署）。
    *   **.NET 部署模式：**
        *   **框架依赖 (Framework-Dependent):** 程序体积小，但用户需预装 .NET 运行时。
        *   **自包含 (Self-Contained):** 打包 .NET 运行时，用户无需安装，但程序体积较大。推荐此方式以简化用户操作。
    *   **配置文件：** 包含一个配置文件（如 `appsettings.json` 或 `config.xml`），用于存储用户设置（快捷键、API Key - 建议加密存储）。程序启动时读取。
    *   **安装程序：** (可选) 使用 Inno Setup, NSIS 或 WiX Toolset 创建安装包，处理快捷方式创建、开机启动项设置（如果提供）、配置文件初始化等。

2.  **测试**
    *   **核心功能测试：**
        *   快捷键触发是否灵敏、准确。
        *   屏幕框选操作是否流畅，选区绘制是否正确，ESC 取消是否有效。
        *   截图是否准确捕获选定区域。
        *   OCR 识别不同字体、背景、语言的准确率。
        *   结果窗口显示是否正确，文本格式是否保留，复制功能是否正常。
        *   窗口拖动、关闭等交互是否符合预期。
    *   **边界与异常测试：**
        *   **多显示器：** 在不同排列、不同 DPI 的多显示器环境下测试框选和截图。
        *   **高 DPI：** 在高缩放比例（如 150%, 200%）下测试坐标计算和 UI 显示。
        *   **特殊区域：** 尝试截取任务栏、开始菜单、其他置顶窗口上的内容。
        *   **网络异常：** 断开网络连接测试 API 调用失败的处理和用户反馈。
        *   **API 错误：** 使用无效/过期的 API Key 测试认证失败；模拟 API 返回错误码测试错误处理。
        *   **无文本图片：** 使用空白图片或纯背景图片测试无结果情况。
        *   **快速连续操作：** 快速按快捷键、快速框选测试程序稳定性。
    *   **兼容性与资源测试：**
        *   在不同 Windows 版本（Win 10, Win 11）上测试。
        *   长时间后台运行，监控内存和 CPU 占用，检查是否有资源泄漏。
    *   **用户体验测试：**
        *   程序启动、退出流程是否顺畅。
        *   托盘图标和菜单是否清晰易用。
        *   设置界面是否直观（如果提供）。
        *   错误提示是否清晰友好。

