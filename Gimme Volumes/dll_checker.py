import os
import shutil
import subprocess
import time
import psutil

APP_DIR = r'C:\Users\zaher\source\repos\Gimme Volumes\Gimme Volumes\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64'  # <-- change this
EXE_NAME = 'Gimme Volumes.exe'
TEMP_DIR = os.path.join(APP_DIR, 'dll_temp')
WAIT_SECONDS = 5

# List of files to skip
# List of files to skip
SKIP_FILES = {
    "System.Diagnostics.Process.dll",
    "System.Drawing.Common.dll",
    "System.Linq.Expressions.dll",
    "System.Memory.dll",
    "System.Numerics.Vectors.dll",
    "System.ObjectModel.dll",
    "System.Private.CoreLib.dll",
    "System.Private.Uri.dll",
    "System.Reflection.Emit.dll",
    "System.Runtime.CompilerServices.Unsafe.dll",
    "System.Runtime.dll",
    "System.Runtime.InteropServices.dll",
    "System.Security.Cryptography.dll",
    "System.Threading.dll",
    "System.Windows.Forms.dll",
    "System.Windows.Forms.Primitives.dll",
    "WinRT.Runtime.dll",
    "WinUIEdit.dll",
    "wuceffectsi.dll",
    "Accessibility.dll",
    "clrjit.dll",
    "coreclr.dll",
    "CoreMessagingXP.dll",
    "dcompi.dll",
    "dwmcorei.dll",
    "exit.png",
    "Gimme Volumes.deps.json",
    "Gimme Volumes.dll",
    "Gimme Volumes.exe",
    "Gimme Volumes.runtimeconfig.json",
    "hostfxr.dll",
    "hostpolicy.dll",
    "icon.ico",
    "Interop.IWshRuntimeLibrary.dll",
    "marshal.dll",
    "Microsoft.DirectManipulation.dll",
    "Microsoft.InputStateManager.dll",
    "Microsoft.InteractiveExperiences.Projection.dll",
    "Microsoft.Internal.FrameworkUdk.dll",
    "Microsoft.UI.Composition.OSSupport.dll",
    "Microsoft.UI.dll",
    "Microsoft.UI.Input.dll",
    "Microsoft.UI.pri",
    "Microsoft.UI.Windowing.Core.dll",
    "Microsoft.UI.Windowing.dll",
    "Microsoft.UI.Xaml.Controls.dll",
    "Microsoft.UI.Xaml.Controls.pri",
    "Microsoft.ui.xaml.dll",
    "Microsoft.ui.xaml.resources.19h1.dll",
    "Microsoft.ui.xaml.resources.common.dll",
    "Microsoft.Win32.Primitives.dll",
    "Microsoft.Windows.ApplicationModel.Resources.dll",
    "Microsoft.Windows.SDK.NET.dll",
    "Microsoft.WindowsAppRuntime.dll",
    "Microsoft.WinUI.dll",
    "MRM.dll",
    "NAudio.Wasapi.dll",
    "netstandard.dll",
    "resources.pri",
    "setting.png",
    "SettingsWindow.xbf",
    "System.Collections.Concurrent.dll",
    "System.Collections.dll",
    "System.ComponentModel.dll",
    "System.ComponentModel.Primitives.dll",
}


def is_app_running():
    for proc in psutil.process_iter(['name']):
        if proc.info['name'] == EXE_NAME:
            return True
    return False

def run_app():
    exe_path = os.path.join(APP_DIR, EXE_NAME)
    return subprocess.Popen(exe_path, cwd=APP_DIR)

def move_and_test_dll(dll_name):
    print(f"\n[Testing] {dll_name}")
    src = os.path.join(APP_DIR, dll_name)
    dst = os.path.join(TEMP_DIR, dll_name)

    # Move DLL out
    shutil.move(src, dst)

    try:
        proc = run_app()
        time.sleep(WAIT_SECONDS)

        if proc.poll() is not None:
            print(f"[❌ Required] {dll_name} - App exited early")
            shutil.move(dst, src)
        elif not is_app_running():
            print(f"[❌ Required] {dll_name} - App not running")
            shutil.move(dst, src)
            proc.kill()
        else:
            print(f"[✅ Not Required] {dll_name}")
            proc.kill()
    except Exception as e:
        print(f"[Error] {dll_name} - {str(e)}")
        shutil.move(dst, src)

def main():
    if not os.path.exists(TEMP_DIR):
        os.makedirs(TEMP_DIR)

    dlls = [f for f in os.listdir(APP_DIR) 
            if f.lower().endswith('.dll') 
            and f not in SKIP_FILES]

    for dll in dlls:
        move_and_test_dll(dll)

    print("\n[Done] DLL testing complete.")

if __name__ == '__main__':
    main()
