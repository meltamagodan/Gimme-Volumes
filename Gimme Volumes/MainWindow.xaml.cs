using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using WinRT.Interop;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

using NAudio.CoreAudioApi;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Gimme_Volumes
{
    public sealed partial class MainWindow : Window
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly MMDevice _defaultDevice;
        private readonly AudioSessionManager _sessionManager;
        private readonly HWND _hWnd;
        private readonly OverlappedPresenter _presenter;
        private readonly AppWindow _appWindow;
        private SessionCollection? sessions;

        //private const int WS_EX_TOOLWINDOW = 0x00000080;
        //private const uint DOT_KEY = 0xBE;
        private const uint WM_HOTKEY = 0x0312;
        private readonly WNDPROC origPrc;
        private readonly WNDPROC hotKeyPrc;

        private NotifyIcon? _trayIcon;
        private ContextMenuStrip? _trayMenu;

        private SettingsWindow? _settingsWindow;

        private static readonly string HotkeyConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gimme_Volumes", "hotkey_config.txt");

        public MainWindow()
        {
            InitializeComponent();

            _hWnd = new HWND(WindowNative.GetWindowHandle(this).ToInt32());
            var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            InitializeTrayIcon();

            InitHotKey();

            hotKeyPrc = HotKeyPrc;
            var hotKeyPrcPointer = Marshal.GetFunctionPointerForDelegate(hotKeyPrc);
            origPrc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(PInvoke.SetWindowLongPtr(_hWnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, hotKeyPrcPointer));

            //int exStyle = PInvoke.GetWindowLong(_hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            //PInvoke.SetWindowLong(_hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
            var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
            var screenBounds = displayArea.WorkArea;

            int windowWidth = 300;
            int windowHeight = 460;

            int targetX = screenBounds.Width - windowWidth;
            int targetY = (screenBounds.Height - windowHeight) / 2;

            _appWindow.MoveAndResize(new Windows.Graphics.RectInt32
            {
                X = targetX,
                Y = targetY,
                Width = windowWidth,
                Height = windowHeight
            });


            _presenter = (OverlappedPresenter)_appWindow.Presenter;
            _presenter.IsMaximizable = false;
            _presenter.IsMinimizable = false;
            _presenter.IsResizable = false;

            SetTitleBar(appTitle);
            
            ExtendsContentIntoTitleBar = true;
            _presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);

            _deviceEnumerator = new MMDeviceEnumerator();
            _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _sessionManager = _defaultDevice.AudioSessionManager;

            CreateSettings();

            LoadAudioSessions();
        }

        private void CreateSettings()
        {
            _settingsWindow = new SettingsWindow(ReRegisterHotkey);
            _settingsWindow.Closed += (s, args) => _settingsWindow = null;
        }

        private void InitHotKey()
        {
            uint key = 0x5A; 
            int modifier = (int)HOT_KEY_MODIFIERS.MOD_ALT;

            if (File.Exists(HotkeyConfigPath))
            {
                var lines = File.ReadAllLines(HotkeyConfigPath);
                modifier = int.Parse(lines[0].Split('=')[1]);
                key = uint.Parse(lines[1].Split('=')[1]);
            } else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HotkeyConfigPath)!);
                File.WriteAllText(HotkeyConfigPath, $"Modifier={modifier}\nKey={key}");
            }

            PInvoke.RegisterHotKey(_hWnd, 0, (HOT_KEY_MODIFIERS)modifier, key);
            
        }

        public void ReRegisterHotkey()
        {
            PInvoke.UnregisterHotKey(_hWnd, 0);
            InitHotKey();
        }

        private void InitializeTrayIcon()
        {
            string exeDirectory = AppContext.BaseDirectory;

            _trayMenu = new ContextMenuStrip();
            var settingPath = Path.Combine(exeDirectory, "Assets\\setting.png");
            var exitPath = Path.Combine(exeDirectory, "Assets\\exit.png");
            var iconPath = Path.Combine(exeDirectory, "Assets\\icon.ico");

            _trayMenu.Items.Add("Settings",
                File.Exists(settingPath) ? System.Drawing.Image.FromFile(settingPath) : null,
                ShowSettings);

            _trayMenu.Items.Add(new ToolStripSeparator());

            _trayMenu.Items.Add("Exit",
                File.Exists(exitPath) ? System.Drawing.Image.FromFile(exitPath) : null,
                ExitApplication);

            _trayIcon = new NotifyIcon
            {
                Icon = File.Exists(iconPath) ? new Icon(iconPath) : null,
                ContextMenuStrip = _trayMenu,
                Visible = true,
                Text = "Gimme Volumes"
            };

            _trayIcon.DoubleClick += (sender, e) => ShowWindow();
        }

        private void ShowSettings(object? sender, EventArgs e)
        {
            if (_settingsWindow == null)
            {
                CreateSettings();
            }
            _settingsWindow?.Activate();
        }

        private void ExitApplication(object? sender, EventArgs e)
        {
            Close();
        }

        private void ShowWindow()
        {
            PInvoke.ShowWindow(_hWnd, SHOW_WINDOW_CMD.SW_SHOW);
            PInvoke.SetForegroundWindow(_hWnd);
            PInvoke.SetFocus(_hWnd);
            PInvoke.SetActiveWindow(_hWnd);
        }

        private LRESULT HotKeyPrc(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            if (uMsg == WM_HOTKEY)
            {
                if (!Visible)
                {
                    PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOW);
                    PInvoke.SetForegroundWindow(hwnd);
                    PInvoke.SetFocus(hwnd);
                    PInvoke.SetActiveWindow(hwnd);
                }
                else
                {
                    PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
                }

                return (LRESULT)IntPtr.Zero;
            }

            return PInvoke.CallWindowProc(origPrc, hwnd, uMsg, wParam, lParam);
        }

        private void LoadAudioSessions()
        {
            try
            {
                _defaultDevice.AudioSessionManager.RefreshSessions();
                
                SessionList.Children.Clear();
                sessions = _defaultDevice.AudioSessionManager.Sessions;

                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    var volume = session.SimpleAudioVolume;
                    string label;

                    // Determine label
                    if (session.IsSystemSoundsSession)
                    {
                        label = "System Sounds";
                    }
                    else if (!string.IsNullOrWhiteSpace(session.DisplayName))
                    {
                        label = session.DisplayName;
                    }
                    else
                    {
                        try
                        {
                            var process = System.Diagnostics.Process.GetProcessById((int)session.GetProcessID);
                            label = process.ProcessName;
                        }
                        catch
                        {
                            label = $"PID {session.GetProcessID}";
                        }
                    }

                    // Create UI elements
                    var container = new StackPanel { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal, Spacing = 10 };
                    var textSliderContainer = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };

                    var nameText = new TextBlock
                    {
                        Text = $"{label}: {(int)(volume.Volume * 100)}%",
                        FontSize = 16,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var slider = new Slider
                    {
                        Minimum = 0,
                        Maximum = 100,
                        Value = volume.Volume * 100,
                        Width = 200,
                        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left
                    };

                    // Create mute button with icon
                    var muteButton = new Microsoft.UI.Xaml.Controls.Button
                    {
                        Width = 32,
                        Height = 32,
                        Padding = new Thickness(0),
                        Background = new SolidColorBrush(Colors.Transparent)
                    };

                    // Store the previous volume before muting
                    float previousVolume = volume.Volume;
                    bool isMuted = volume.Volume == 0;

                    // Set initial icon
                    var soundIcon = new FontIcon
                    {
                        Glyph = volume.Mute ? "\uE74F" : GetVolumeIcon(volume.Volume),
                        FontSize = 16
                    };
                    muteButton.Content = soundIcon;

                    muteButton.Click += (s, e) =>
                    {
                        if (isMuted)
                        {
                            // Unmute (restore previous volume)
                            volume.Volume = previousVolume;
                            slider.Value = previousVolume * 100;
                            soundIcon.Glyph = GetVolumeIcon(previousVolume);
                            nameText.Text = $"{label}: {(int)(previousVolume * 100)}%";
                            isMuted = false;
                        }
                        else
                        {
                            // Mute (store volume, set to 0)
                            previousVolume = volume.Volume;
                            volume.Volume = 0;
                            slider.Value = 0;
                            soundIcon.Glyph = "\uE74F"; // Muted icon
                            nameText.Text = $"{label}: (Muted)";
                            isMuted = true;
                        }
                    };

                    slider.ValueChanged += (s, e) =>
                    {
                        float newVolume = (float)(e.NewValue / 100);
                        volume.Volume = newVolume;

                        if (newVolume == 0)
                        {
                            isMuted = true;
                            soundIcon.Glyph = "\uE74F";
                            nameText.Text = $"{label}: (Muted)";
                        }
                        else
                        {
                            isMuted = false;
                            previousVolume = newVolume;
                            soundIcon.Glyph = GetVolumeIcon(newVolume);
                            nameText.Text = $"{label}: {(int)e.NewValue}%";
                        }
                    };

                    

                    textSliderContainer.Children.Add(nameText);
                    textSliderContainer.Children.Add(slider);

                    container.Children.Add(muteButton);
                    container.Children.Add(textSliderContainer);

                    SessionList.Children.Add(container);
                }

                if (SessionList.Children.Count == 0)
                {
                    SessionList.Children.Add(new TextBlock
                    {
                        Text = "No active audio sessions.",
                        FontSize = 16,
                        Foreground = new SolidColorBrush(Colors.Gray)
                    });
                }
            }
            catch (Exception ex)
            {
                SessionList.Children.Clear();
                SessionList.Children.Add(new TextBlock
                {
                    Text = $"Error loading sessions: {ex.Message}",
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Colors.Red)
                });
            }
        }

        private static string GetVolumeIcon(float volume)
        {
            if (volume <= 0) return "\uE74F"; // Muted
            if (volume < 0.33) return "\uE993"; // Volume 1
            if (volume < 0.66) return "\uE994"; // Volume 2
            return "\uE995"; // Volume 3
        }

        /*private void SaveWindowBounds()
        {
            var bounds = _appWindow.Position;
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Gimme_Volumes", "window_bounds.txt");

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, $"{bounds.X},{bounds.Y}");
        }*/

        /*private void RestoreWindowBounds()
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Gimme_Volumes", "window_bounds.txt");

            if (File.Exists(configPath))
            {
                var contents = File.ReadAllText(configPath);
                var parts = contents.Split(',');

                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int x) &&
                    int.TryParse(parts[1], out int y))
                {
                    _appWindow.Move(new Windows.Graphics.PointInt32 { X = x, Y = y });
                }
            }
        }*/

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            if (_settingsWindow != null)
            {
                _settingsWindow?.Close();
            }
            //SaveWindowBounds();
            PInvoke.SetWindowLongPtr(_hWnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(origPrc));
            _trayIcon?.Dispose();
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            LoadAudioSessions();
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                PInvoke.ShowWindow(_hWnd, SHOW_WINDOW_CMD.SW_HIDE);
            }
        }

        private void Button_Settings(object sender, RoutedEventArgs e)
        {
            if (_settingsWindow == null)
            {
                CreateSettings();
            }
            _settingsWindow?.Activate();
        }

        private void Button_Refresh(object sender, RoutedEventArgs e)
        {
            LoadAudioSessions();
        }

    }

}
