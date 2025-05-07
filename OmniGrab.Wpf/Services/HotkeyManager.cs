using System;
using System.Runtime.InteropServices;
using System.Windows.Forms; // For Keys enum and Message
using System.ComponentModel; // For Win32Exception
using System.Windows.Interop; // Needed for NativeWindow in WPF context potentially, and HwndSource

namespace OmniGrab.Wpf.Services;

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

public class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409; // Win32 Error Code

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly int _hotkeyId;
    private IntPtr _windowHandle; // Handle of the window listening for hotkeys
    private HwndSource? _source;
    private bool _isRegistered = false;
    private bool _disposed = false;

    public event EventHandler HotkeyPressed = delegate { };

    public HotkeyManager(System.Windows.Window window)
    {
        _hotkeyId = GetHashCode();
        var interopHelper = new WindowInteropHelper(window);
        _windowHandle = interopHelper.EnsureHandle(); // Get handle safely

        if (_windowHandle == IntPtr.Zero)
        {
            Console.WriteLine("Warning: Window handle is Zero during HotkeyManager construction.");
        }
        else
        {
            _source = HwndSource.FromHwnd(_windowHandle);
            if (_source != null)
            {
                _source.AddHook(HwndHook);
            }
            else
            {
                Console.WriteLine("Warning: Could not get HwndSource for hotkey registration.");
            }
        }
    }

    public void Register(Keys key, ModifierKeys modifiers)
    {
        if (_windowHandle == IntPtr.Zero)
        {
             throw new InvalidOperationException("Window handle is not valid for registering hotkeys.");
        }

        if (_isRegistered)
        {
            Unregister(); // Unregister previous if any
        }

        if (!RegisterHotKey(_windowHandle, _hotkeyId, (uint)modifiers, (uint)key))
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

    public void Unregister()
    {
        if (_isRegistered && _windowHandle != IntPtr.Zero)
        {
            if (!UnregisterHotKey(_windowHandle, _hotkeyId))
            {
                int errorCode = Marshal.GetLastWin32Error();
                Console.WriteLine($"Warning: Failed to unregister hotkey (ID: {_hotkeyId}). Win32 Error: {errorCode}");
            }
            _isRegistered = false;
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            if (wParam.ToInt32() == _hotkeyId)
            {
                HotkeyPressed(this, EventArgs.Empty);
                handled = true;
            }
        }
        return IntPtr.Zero;
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
                _source?.RemoveHook(HwndHook);
                _source?.Dispose();
            }

            Unregister();

            _disposed = true;
        }
    }

     ~HotkeyManager()
     {
        Dispose(false);
     }
} 