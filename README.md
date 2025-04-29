# CopyEverythingOcr - Windows 屏幕 OCR 小工具

**一个简单的 Windows 应用程序，可以通过快捷键截取屏幕选定区域，调用 OCR API (兼容 OpenAI Vision API 格式) 识别其中的文本，并将结果显示在一个可拖动的小窗口中以便复制。**

## 功能特性

*   **全局快捷键触发:** 通过可配置的全局快捷键 (默认为 `Ctrl + Alt + F1`) 启动截图。
*   **屏幕区域选择:** 快捷键触发后，屏幕变暗，允许用户通过鼠标拖拽选择要识别的区域。按 `ESC` 可取消。
*   **OCR 识别:** 将截取的图像发送给配置的 OCR API 服务 (需兼容 OpenAI Vision API 格式) 进行文本识别。
*   **自定义 API 端点:** 用户可以在配置文件中指定使用的 OCR API URL 和模型名称，方便使用 OpenAI 或其他兼容服务。
*   **结果展示窗口:** 在截图区域附近弹出一个置顶、无边框的小窗口，显示识别结果。
*   **一键复制:** 结果窗口提供“复制”按钮，方便将识别文本复制到剪贴板。
*   **窗口拖动:** 结果窗口可以随意拖动。

## 技术栈

*   **语言:** C#
*   **框架:** .NET 9 (或项目配置的版本), WPF (Windows Presentation Foundation)
*   **库:**
    *   `System.Drawing.Common` (用于屏幕截图)
    *   `Newtonsoft.Json` (用于处理 API 响应)
    *   `Microsoft.Extensions.Configuration` (用于加载 `appsettings.json` 配置)

## 环境要求

*   **运行:** Windows 10 或更高版本，安装了对应版本的 .NET Desktop Runtime (如果项目发布为框架依赖模式)。如果发布为自包含模式则无需额外安装。
*   **开发/构建:** .NET 9 SDK (或项目使用的 SDK 版本)。

## 配置

应用程序的主要配置存储在 `appsettings.json` 文件中。在运行程序前，请确保此文件存在于程序运行目录下，并且已正确配置。

```json
{
  "Settings": {
    "Ocr": {
      "ApiKey": "YOUR_API_KEY_HERE", // <--- 替换为您 OCR 服务的 API Key
      "SecretKey": "", // 如果您的服务需要 Secret Key，请在此处配置 (当前代码未使用)
      "OcrEndpointUrl": "YOUR_OPENAI_COMPATIBLE_ENDPOINT_URL_HERE", // <--- 替换为您 OCR 服务的完整 URL (例如: https://api.openai.com/v1/chat/completions)
      "OcrModelName": "gpt-4-vision-preview" // <--- 替换为您想使用的模型名称 (可选, 默认会用 gpt-4-vision-preview)
    },
    "Hotkey": {
      "Key": "F1", // 触发截图的热键 (例如 F1, S, PrintScreen 等)
      "Modifiers": "Control, Alt" // 触发截图的修饰键 (可以是 None, Control, Alt, Shift, Win 的组合，用逗号或空格分隔)
    },
    "ResultWindow": {
      "FontSize": 12, // 结果窗口字体大小 (当前代码暂未实现应用此设置)
      "AutoCopy": false // 是否自动复制识别结果到剪贴板 (当前代码暂未实现应用此设置)
    }
  }
}
```

**重要:**

1.  **替换占位符:** 务必将 `"YOUR_API_KEY_HERE"` 和 `"YOUR_OPENAI_COMPATIBLE_ENDPOINT_URL_HERE"` 替换为您实际使用的值。
2.  **复制到输出目录:** 确保 `appsettings.json` 文件在项目中的属性设置为“复制到输出目录: 如果较新则复制”或“始终复制”，这样程序运行时才能找到它。

## 构建与运行

1.  **克隆仓库 (如果适用):** `git clone <repository-url>`
2.  **进入项目目录:** `cd CopyEverythingOcr.Wpf` (或项目根目录)
3.  **还原依赖:** `dotnet restore`
4.  **构建:** `dotnet build`
5.  **运行:** `dotnet run --project CopyEverythingOcr.Wpf/CopyEverythingOcr.Wpf.csproj`
    *   或者直接在 Visual Studio 中按 F5 运行。

## 如何使用

1.  确保已正确配置 `appsettings.json` 并将其复制到输出目录。
2.  运行应用程序 (`dotnet run ...` 或从 Visual Studio 启动)。会显示一个空白的 `MainWindow` 窗口 (后续可配置为隐藏)。
3.  按下您在 `appsettings.json` 中配置的快捷键 (默认为 `Ctrl + Alt + F1`)。
4.  屏幕会变暗，鼠标光标变为十字准星。
5.  按住鼠标左键并拖动，选择您想识别文字的屏幕区域。
6.  松开鼠标左键。
7.  程序会将截图发送给配置的 OCR API。
8.  稍等片刻，如果识别成功，会在您截图区域的附近弹出一个小窗口，显示识别结果。
9.  您可以在结果窗口中：
    *   阅读识别的文本。
    *   点击 "Copy" 按钮将文本复制到剪贴板。
    *   点击 "✕" 按钮关闭结果窗口。
    *   按住窗口的任意位置拖动它。
10. 您可以重复按快捷键进行新的识别。新的结果窗口出现时，旧的结果窗口会自动关闭。

## (可选) 未来可添加的功能

*   系统托盘图标及菜单 (后台运行、设置、退出)。
*   设置界面，用于图形化配置快捷键和 API 信息。
*   结果窗口应用配置中的字体大小和自动复制选项。
*   更完善的错误提示和日志记录。
*   支持更多 OCR 服务商的 API 格式。
*   结果编辑功能。
*   创建安装程序。
