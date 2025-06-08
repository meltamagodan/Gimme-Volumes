using IWshRuntimeLibrary;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Graphics;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32.Foundation;
using WinRT.Interop;

namespace Gimme_Volumes
{
    public sealed partial class SettingsWindow : Window
    {
        private static readonly string StartupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        private static readonly string ShortcutPath = Path.Combine(StartupFolderPath, $"GimmeVolumes.lnk");
        private static readonly string HotkeyConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Gimme_Volumes", "hotkey_config.txt");

        private readonly Action _onHotkeySaved;
        private readonly HWND hwnd;

        public SettingsWindow(Action onHotkeySaved)
        {
            InitializeComponent();

            _onHotkeySaved = onHotkeySaved;

            hwnd = new HWND(WindowNative.GetWindowHandle(this).ToInt32());
            InitWindowStyle();

            StartupToggle.IsOn = IsStartupEnabled();
            StartupToggle.Toggled += (s, e) =>
            {
                SetStartup(StartupToggle.IsOn);
            };

            LoadHotkeyUI();
        }

        private void InitWindowStyle()
        {
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            PointInt32 CenteredPosition = appWindow.Position;
            CenteredPosition.X = (displayArea.WorkArea.Width + 320) / 2;
            CenteredPosition.Y = (displayArea.WorkArea.Height - 320) / 2;
            appWindow.Move(CenteredPosition);
            appWindow.Resize(new SizeInt32 { Width = 300, Height = 200 });

            var _presenter = (OverlappedPresenter)appWindow.Presenter;
            _presenter.IsMaximizable = false;
            _presenter.IsMinimizable = false;
            _presenter.IsResizable = false;
            _presenter.IsAlwaysOnTop = true;

            ExtendsContentIntoTitleBar = true;
        }

        private void LoadHotkeyUI()
        {
            if (System.IO.File.Exists(HotkeyConfigPath))
            {
                var lines = System.IO.File.ReadAllLines(HotkeyConfigPath);
                int modifier = int.Parse(lines[0].Split('=')[1]);
                uint key = uint.Parse(lines[1].Split('=')[1]);
                HotkeyRecordButton.Content = $"{FormatModifier(modifier)}{GetKeyName(key)}";
            }
            else
            {
                HotkeyRecordButton.Content = "Click to assign";
            }
        }

        private static void SetStartup(bool enable)
        {
            string exePath = Environment.ProcessPath!;

            if (enable)
            {
                var shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(ShortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.WindowStyle = 1;
                shortcut.Description = "Launch Gimme Volumes at startup";
                shortcut.Save();
            }
            else
            {
                if (System.IO.File.Exists(ShortcutPath))
                {
                    System.IO.File.Delete(ShortcutPath);
                }
            }
        }

        private static bool IsStartupEnabled()
        {
            return System.IO.File.Exists(ShortcutPath);
        }

        private bool isRecordingHotkey = false;
        private uint currentKeyCode = 0;
        private int currentModifier = 0;

        private void SaveDetectedHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (currentKeyCode == 0) return;

            Directory.CreateDirectory(Path.GetDirectoryName(HotkeyConfigPath)!);
            System.IO.File.WriteAllText(HotkeyConfigPath, $"Modifier={currentModifier}\nKey={currentKeyCode}");

            SaveDetectedHotkeyButton.Visibility = Visibility.Collapsed;
            CancelHotkeyButton.Visibility = Visibility.Collapsed;
            isRecordingHotkey = false;
            _onHotkeySaved?.Invoke();

        }

        private void HotkeyRecordButton_Click(object sender, RoutedEventArgs e)
        {
            isRecordingHotkey = true;
            SaveDetectedHotkeyButton.IsEnabled = false;
            currentKeyCode = 0;
            currentModifier = 0;
            HotkeyRecordButton.Content = "Press a key...";
            SaveDetectedHotkeyButton.Visibility = Visibility.Visible;
            CancelHotkeyButton.Visibility = Visibility.Visible;

            // Set focus so KeyDown is detected
            HotkeyRecordButton.Focus(FocusState.Programmatic);
        }

        private void CancelHotkey_Click(object sender, RoutedEventArgs e)
        {
            isRecordingHotkey = false;
            currentKeyCode = 0;
            currentModifier = 0;
            LoadHotkeyUI(); // Re-loads the last saved hotkey
            SaveDetectedHotkeyButton.Visibility = Visibility.Collapsed;
            CancelHotkeyButton.Visibility = Visibility.Collapsed;
        }


        private static string FormatModifier(int mod)
        {
            List<string> parts = [];
            if ((mod & 2) != 0) parts.Add("Ctrl");
            if ((mod & 1) != 0) parts.Add("Alt");
            if ((mod & 4) != 0) parts.Add("Shift");
            if ((mod & 8) != 0) parts.Add("Win");
            return parts.Count > 0 ? string.Join("+", parts) + "+" : "";
        }

        private static string GetKeyName(uint key)
        {
            var knownKeys = new Dictionary<uint, string>
            {
                [0x00] = "",
                [0x08] = "Backspace",
                [0x09] = "Tab",
                [0x0D] = "Enter",
                [0x1B] = "Esc",
                [0x20] = "Space",
                [0x25] = "Left",
                [0x26] = "Up",
                [0x27] = "Right",
                [0x28] = "Down",
                [0x30] = "0",
                [0x31] = "1",
                [0x32] = "2",
                [0x33] = "3",
                [0x34] = "4",
                [0x35] = "5",
                [0x36] = "6",
                [0x37] = "7",
                [0x38] = "8",
                [0x39] = "9",
                [0x41] = "A",
                [0x42] = "B",
                [0x43] = "C",
                [0x44] = "D",
                [0x45] = "E",
                [0x46] = "F",
                [0x47] = "G",
                [0x48] = "H",
                [0x49] = "I",
                [0x4A] = "J",
                [0x4B] = "K",
                [0x4C] = "L",
                [0x4D] = "M",
                [0x4E] = "N",
                [0x4F] = "O",
                [0x50] = "P",
                [0x51] = "Q",
                [0x52] = "R",
                [0x53] = "S",
                [0x54] = "T",
                [0x55] = "U",
                [0x56] = "V",
                [0x57] = "W",
                [0x58] = "X",
                [0x59] = "Y",
                [0x5A] = "Z",
                [0x70] = "F1",
                [0x71] = "F2",
                [0x72] = "F3",
                [0x73] = "F4",
                [0x74] = "F5",
                [0x75] = "F6",
                [0x76] = "F7",
                [0x77] = "F8",
                [0x78] = "F9",
                [0x79] = "F10",
                [0x7A] = "F11",
                [0x7B] = "F12"
            };
            return knownKeys.TryGetValue(key, out var name) ? name : key.ToString();
        }

        private void StackPanel_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (!isRecordingHotkey) return;

            var source = InputKeyboardSource.GetKeyStateForCurrentThread;

            bool isCtrl = source(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool isShift = source(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            bool isAlt = source(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
            bool isWin = source(VirtualKey.LeftWindows).HasFlag(CoreVirtualKeyStates.Down)
                        || source(VirtualKey.RightWindows).HasFlag(CoreVirtualKeyStates.Down);

            VirtualKey keyPressed = e.Key;
            uint keyCode = (uint)keyPressed;
            
            if (keyCode == 16 || keyCode == 17 || keyCode == 18 || keyCode == 91 || keyCode == 92)
                keyCode = 0;
            

            int modifier = 0;
            if (isCtrl) modifier |= 2;
            if (isAlt) modifier |= 1;
            if (isShift) modifier |= 4;
            if (isWin) modifier |= 8;

            currentKeyCode = keyCode;
            currentModifier = modifier;

            if (currentKeyCode == 0)
                SaveDetectedHotkeyButton.IsEnabled = false;
            else
                SaveDetectedHotkeyButton.IsEnabled = true;

            HotkeyRecordButton.Content = $"{FormatModifier(modifier)}{GetKeyName(keyCode)}";
            e.Handled = true;

        }
    }
}
