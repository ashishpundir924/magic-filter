using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Win32;
using MessageBox = System.Windows.Forms.MessageBox;

namespace MagicDefor.Installer;

internal static class Program
{
    private const string ProductName = "DEFOR Live Filter";
    private const string ProductVersion = "1.0.0";
    private const string Publisher = "DEFOR";
    private const string RevitVersion = "2023";

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var installRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                ProductName,
                $"Revit {RevitVersion}");

            var manifestPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Autodesk",
                "Revit",
                "Addins",
                RevitVersion,
                "MagicDefor.LiveFilter.addin");

            if (args.Any(arg => string.Equals(arg, "--uninstall", StringComparison.OrdinalIgnoreCase)))
            {
                Uninstall(installRoot, manifestPath);
                return 0;
            }

            Install(installRoot, manifestPath);
            return 0;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Installation failed.\n\n{exception.Message}",
                "DEFOR Live Filter Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void Install(string installRoot, string manifestPath)
    {
        Directory.CreateDirectory(installRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);

        var currentExe = Environment.ProcessPath ?? throw new InvalidOperationException("Installer path was not available.");
        var targetExe = Path.Combine(installRoot, "DEFOR.LiveFilter.Setup.exe");
        var targetDll = Path.Combine(installRoot, "MagicDefor.Revit.dll");
        var manifestContents = BuildManifest(targetDll);

        File.Copy(currentExe, targetExe, true);
        ExtractEmbeddedDll(targetDll);
        File.WriteAllText(Path.Combine(installRoot, "MagicDefor.LiveFilter.addin"), manifestContents, Encoding.UTF8);
        File.WriteAllText(manifestPath, manifestContents, Encoding.UTF8);

        WriteUninstallRegistry(installRoot, targetExe, targetDll);

        MessageBox.Show(
            $"Installed successfully.\n\nLocation:\n{installRoot}\n\nThe add-in is now available in Revit {RevitVersion}.",
            "DEFOR Live Filter Setup",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void Uninstall(string installRoot, string manifestPath)
    {
        RemoveUninstallRegistry();

        if (File.Exists(manifestPath))
        {
            File.Delete(manifestPath);
        }

        if (Directory.Exists(installRoot))
        {
            var cleanupScriptPath = Path.Combine(Path.GetTempPath(), $"DEFOR.LiveFilter.Cleanup.{Guid.NewGuid():N}.cmd");
            var cleanupScript = $@"@echo off
ping 127.0.0.1 -n 2 > nul
rmdir /s /q ""{installRoot}""
del /f /q ""%~f0""";
            File.WriteAllText(cleanupScriptPath, cleanupScript, Encoding.ASCII);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{cleanupScriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        MessageBox.Show(
            "DEFOR Live Filter was uninstalled for the current user.",
            "DEFOR Live Filter Setup",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void WriteUninstallRegistry(string installRoot, string installerExe, string displayIconPath)
    {
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{ProductName}");
        key?.SetValue("DisplayName", ProductName);
        key?.SetValue("DisplayVersion", ProductVersion);
        key?.SetValue("Publisher", Publisher);
        key?.SetValue("InstallLocation", installRoot);
        key?.SetValue("DisplayIcon", displayIconPath);
        key?.SetValue("UninstallString", $"\"{installerExe}\" --uninstall");
        key?.SetValue("QuietUninstallString", $"\"{installerExe}\" --uninstall");
        key?.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key?.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key?.SetValue("EstimatedSize", (int)(new FileInfo(displayIconPath).Length / 1024), RegistryValueKind.DWord);
    }

    private static void RemoveUninstallRegistry()
    {
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{ProductName}", false);
    }

    private static string BuildManifest(string assemblyPath)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""no""?>
<RevitAddIns>
  <AddIn Type=""Application"">
    <Name>{ProductName}</Name>
    <Assembly>{assemblyPath}</Assembly>
    <AddInId>9C1A4B25-2D9F-4984-A165-4E574F31EE10</AddInId>
    <FullClassName>MagicDefor.Revit.App</FullClassName>
    <VendorId>MDEF</VendorId>
    <VendorDescription>DEFOR advanced live filter tools for Revit.</VendorDescription>
  </AddIn>
</RevitAddIns>";
    }

    private static void ExtractEmbeddedDll(string targetDll)
    {
        const string resourceName = "MagicDefor.Installer.Payload.MagicDefor.Revit.dll";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException("Embedded add-in payload was not found inside the installer.");
        }

        using var file = File.Create(targetDll);
        stream.CopyTo(file);
    }
}
