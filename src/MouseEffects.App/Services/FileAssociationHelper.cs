using Microsoft.Win32;
using MouseEffects.Core.Diagnostics;

namespace MouseEffects.App.Services;

/// <summary>
/// Handles per-user file association for .me settings files.
/// Uses HKEY_CURRENT_USER so no admin rights are required.
/// </summary>
public static class FileAssociationHelper
{
    private const string Extension = ".me";
    private const string ProgId = "MouseEffects.SettingsFile";
    private const string FileTypeDescription = "MouseEffects Settings File";

    /// <summary>
    /// Register .me file association for the current user.
    /// </summary>
    public static void RegisterFileAssociation()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                Logger.Log("FileAssociation", "Cannot register: ProcessPath is null");
                return;
            }

            // Check if already registered with current exe path
            if (IsRegisteredCorrectly(exePath))
            {
                Logger.Log("FileAssociation", "File association already registered correctly");
                return;
            }

            // Register the extension -> ProgId mapping
            using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Extension}"))
            {
                extKey?.SetValue("", ProgId);
            }

            // Register the ProgId with description and icon
            using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
            {
                progIdKey?.SetValue("", FileTypeDescription);

                // Set default icon to the application icon
                using (var iconKey = progIdKey?.CreateSubKey("DefaultIcon"))
                {
                    iconKey?.SetValue("", $"\"{exePath}\",0");
                }

                // Set open command
                using (var commandKey = progIdKey?.CreateSubKey(@"shell\open\command"))
                {
                    commandKey?.SetValue("", $"\"{exePath}\" \"%1\"");
                }
            }

            // Notify shell of change
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            Logger.Log("FileAssociation", $"Registered .me file association for: {exePath}");
        }
        catch (Exception ex)
        {
            Logger.Log("FileAssociation", $"Failed to register file association: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if file association is already registered correctly.
    /// </summary>
    private static bool IsRegisteredCorrectly(string exePath)
    {
        try
        {
            using var extKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{Extension}");
            if (extKey == null) return false;

            var progId = extKey.GetValue("") as string;
            if (progId != ProgId) return false;

            using var commandKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProgId}\shell\open\command");
            if (commandKey == null) return false;

            var command = commandKey.GetValue("") as string;
            if (string.IsNullOrEmpty(command)) return false;

            // Check if the registered exe path matches current exe
            return command.Contains(exePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Unregister the .me file association.
    /// </summary>
    public static void UnregisterFileAssociation()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{Extension}", throwOnMissingSubKey: false);
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", throwOnMissingSubKey: false);

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            Logger.Log("FileAssociation", "Unregistered .me file association");
        }
        catch (Exception ex)
        {
            Logger.Log("FileAssociation", $"Failed to unregister file association: {ex.Message}");
        }
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
