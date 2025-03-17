using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Forms; // Required for MessageBox

class Installer
{
  static string GetInstallPath()
  {
    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    return Path.Combine(localAppData, "Remote Desktop Protocol Handler");
  }

  static void CopyLauncher(string installPath)
  {
    string exeSource = "launcher.exe"; // Ensure this file exists in the same folder
    string exeDest = Path.Combine(installPath, "launcher.exe");

    if (!Directory.Exists(installPath))
    {
      Directory.CreateDirectory(installPath);
    }

    File.Copy(exeSource, exeDest, true);
  }

  static void RegisterProtocol(string exePath)
  {
    string baseKey = @"Software\Classes\rdp";
    RegistryKey key = null;

    try
    {
      key = Registry.CurrentUser.CreateSubKey(baseKey);
      key.SetValue("", "URL:Remote Desktop Protocol");
      key.SetValue("URL Protocol", "");
    }
    finally
    {
      if (key != null) key.Close();
    }

    RegistryKey commandKey = null;
    try
    {
      commandKey = Registry.CurrentUser.CreateSubKey(baseKey + @"\shell\open\command");
      commandKey.SetValue("", "\"" + exePath + "\" \"%1\"");
    }
    finally
    {
      if (commandKey != null) commandKey.Close();
    }
  }
  static void CreateUninstaller(string installPath)
  {
    string uninstallerPath = Path.Combine(installPath, "uninstall.bat");

    using (StreamWriter sw = new StreamWriter(uninstallerPath))
    {
      sw.WriteLine("@echo off");
      sw.WriteLine("reg delete HKCU\\Software\\Classes\\rdp /f");
      sw.WriteLine("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\RDPLauncher /f");
      sw.WriteLine("rmdir /s /q \"" + installPath + "\"");
      sw.WriteLine("echo Uninstallation complete.");
      sw.WriteLine("pause");
    }

    // Calculate the folder size in KB
    long estimatedSizeKB = CalculateFolderSize(installPath) / 1024;

    // Register the uninstaller with Windows
    RegistryKey uninstallKey = null;
    try
    {
      uninstallKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\RDPLauncher");
      uninstallKey.SetValue("DisplayName", "Remote Desktop Protocol Handler");
      uninstallKey.SetValue("UninstallString", "\"" + uninstallerPath + "\"");
      uninstallKey.SetValue("DisplayVersion", "1.0");
      uninstallKey.SetValue("Publisher", "Jack Buehner");
      uninstallKey.SetValue("InstallLocation", installPath);
      uninstallKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
      uninstallKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
      uninstallKey.SetValue("EstimatedSize", (int)estimatedSizeKB, RegistryValueKind.DWord);
    }
    finally
    {
      if (uninstallKey != null) uninstallKey.Close();
    }
  }

  static long CalculateFolderSize(string folderPath)
  {
    long totalSize = 0;
    try
    {
      if (Directory.Exists(folderPath))
      {
        foreach (string file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
        {
          FileInfo fileInfo = new FileInfo(file);
          totalSize += fileInfo.Length;
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("Error calculating folder size: " + ex.Message);
    }
    return totalSize;
  }

  static void Main()
  {
    string installPath = GetInstallPath();

    try
    {
      Console.WriteLine("Installing to: " + installPath);
      CopyLauncher(installPath);

      string exePath = Path.Combine(installPath, "launcher.exe");
      RegisterProtocol(exePath);
      CreateUninstaller(installPath);

      Console.WriteLine("Installation complete. RDP protocol registered.");

      // Show success message box
      MessageBox.Show("Installation of Remote Desktop Protocol Handler finished.", "Installation Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
      Console.WriteLine("Error: " + ex.Message);
      MessageBox.Show("Installation failed:\n" + ex.Message, "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }
}
