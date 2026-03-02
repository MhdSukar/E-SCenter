using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace ESCenter.Services
{
    public static class WindowsStartupService
    {
        private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "ESCenter";

        public static bool SetLaunchAtStartup(bool enabled)
        {
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true);
                if (runKey is null)
                {
                    return false;
                }

                if (enabled)
                {
                    var executablePath = GetExecutablePath();
                    if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                    {
                        return false;
                    }

                    runKey.SetValue(AppRegistryName, $"\"{executablePath}\"");
                }
                else
                {
                    runKey.DeleteValue(AppRegistryName, false);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsLaunchAtStartupEnabled()
        {
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
                if (runKey is null)
                {
                    return false;
                }

                var value = runKey.GetValue(AppRegistryName) as string;
                return !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                return false;
            }
        }

        private static string GetExecutablePath()
        {
            var processPath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                return processPath;
            }

            var assemblyPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            return assemblyPath ?? string.Empty;
        }
    }
}
