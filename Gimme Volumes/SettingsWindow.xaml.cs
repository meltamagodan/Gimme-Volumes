using IWshRuntimeLibrary;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Graphics;
using Windows.Win32.Foundation;
using WinRT.Interop;
using System.Linq;

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
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            PointInt32 CenteredPosition = appWindow.Position;
            CenteredPosition.X = (displayArea.WorkArea.Width+320) / 2;
            CenteredPosition.Y = (displayArea.WorkArea.Height-320) / 2;
            appWindow.Move(CenteredPosition);
            appWindow.Resize(new SizeInt32 { Width = 280, Height = 320 });

            var _presenter = (OverlappedPresenter)appWindow.Presenter;
            _presenter.IsMaximizable = false;
            _presenter.IsMinimizable = false;
            _presenter.IsResizable = false;
            
            ExtendsContentIntoTitleBar = true;

            StartupToggle.IsOn = IsStartupEnabled();
            StartupToggle.Toggled += (s, e) =>
            {
                SetStartup(StartupToggle.IsOn);
            };

            LoadHotkeyUI();
        }

        private void SaveHotkey_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(HotkeyConfigPath);
            int modifier = 0;
            if (AltToggle.IsChecked == true) modifier |= 1;
            if (CtrlToggle.IsChecked == true) modifier |= 2;
            if (ShiftToggle.IsChecked == true) modifier |= 4;
            if (WinToggle.IsChecked == true) modifier |= 8;

            if (KeyDropdown.SelectedItem is ComboBoxItem selectedItem &&
                uint.TryParse(selectedItem.Tag?.ToString(), out uint key))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HotkeyConfigPath)!);
                System.IO.File.WriteAllText(HotkeyConfigPath, $"Modifier={modifier}\nKey={key}");
            }

            _onHotkeySaved?.Invoke();
        }

        private void LoadHotkeyUI()
        {
            KeyDropdown.Items.Clear();

            PopulateKeyDropdown();

            if (System.IO.File.Exists(HotkeyConfigPath))
            {
                var lines = System.IO.File.ReadAllLines(HotkeyConfigPath);
                int modifier = int.Parse(lines[0].Split('=')[1]);
                uint key = uint.Parse(lines[1].Split('=')[1]);

                AltToggle.IsChecked = (modifier & 1) != 0;
                CtrlToggle.IsChecked = (modifier & 2) != 0;
                ShiftToggle.IsChecked = (modifier & 4) != 0;
                WinToggle.IsChecked = (modifier & 8) != 0;
                foreach (var item in from ComboBoxItem item in KeyDropdown.Items
                                     where item.Tag?.ToString() == key.ToString()
                                     select item)
                {
                    KeyDropdown.SelectedItem = item;
                    break;
                }
            }
        }

        private void PopulateKeyDropdown()
        {
            var keyMap = new Dictionary<uint, string>
            {
                [0x08] = "Backspace",
                [0x09] = "Tab",
                [0x0D] = "Enter",
                
                [0x1B] = "Esc",
                [0x20] = "Space",
                [0x21] = "Page Up",
                [0x22] = "Page Down",
                [0x23] = "End",
                [0x24] = "Home",
                [0x25] = "Left",
                [0x26] = "Up",
                [0x27] = "Right",
                [0x28] = "Down",
                [0x2C] = "Print Screen",
                [0x2D] = "Insert",
                [0x2E] = "Delete",
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
                [0x60] = "Num 0",
                [0x61] = "Num 1",
                [0x62] = "Num 2",
                [0x63] = "Num 3",
                [0x64] = "Num 4",
                [0x65] = "Num 5",
                [0x66] = "Num 6",
                [0x67] = "Num 7",
                [0x68] = "Num 8",
                [0x69] = "Num 9",
                [0x6A] = "Num *",
                [0x6B] = "Num +",
                [0x6D] = "Num -",
                [0x6E] = "Num Del",
                [0x6F] = "Num /",
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
                [0x7B] = "F12",
                
                [0xBA] = ";",
                [0xBB] = "=",
                [0xBC] = ",",
                [0xBD] = "-",
                [0xBE] = ".",
                [0xBF] = "/",
                [0xC0] = "`",
                [0xDB] = "[",
                [0xDC] = "\\",
                [0xDD] = "]",
                [0xDE] = "'"
            };

            foreach (var kvp in keyMap)
            {
                KeyDropdown.Items.Add(new ComboBoxItem
                {
                    Content = kvp.Value,
                    Tag = kvp.Key
                });
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

    }
}
