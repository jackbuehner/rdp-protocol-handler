using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Windows.Forms;

class RdpLauncher
{
  /* The `rdpOptions` dictionary is used to determine valid keys. */
  static Dictionary<string, string> rdpOptions = new Dictionary<string, string>
  {
    { "allow desktop composition", "i:<0 or 1>" },
    { "allow font smoothing", "i:<0 or 1>" },
    { "alternate shell", "s:<string>" },
    { "audiomode", "i:<0, 1, or 2>" },
    { "authentication level", "i:<0, 1, or 2>" },
    { "connect to console", "i:<0 or 1>" },
    { "disable cursor settings", "i:<0 or1>" },
    { "disable full window drag", "i:<0 or 1>" },
    { "disable menu anims", "i:<0 or 1>" },
    { "disable themes", "i:<0 or 1>" },
    { "disable wallpaper", "i:<0 or 1>" },
    { "drivestoredirect", "s:* (this is the only supported value)" },
    { "desktopheight", "i:<value in pixels>" },
    { "desktopwidth", "i:<value in pixels>" },
    { "domain", "s:<string>" },
    { "full address", "s:<string>" },
    { "gatewayhostname", "s:<string>" },
    { "gatewayusagemethod", "i:<1 or 2>" },
    { "prompt for credentials on client", "i:<0 or 1>" },
    { "loadbalanceinfo", "s:<string>" },
    { "redirectprinters", "i:<0 or 1>" },
    { "remoteapplicationcmdline", "s:<string>" },
    { "remoteapplicationmode", "i:<0 or 1>" },
    { "remoteapplicationprogram", "s:<string>" },
    { "shell working directory", "s:<string>" },
    { "Use redirection server name", "i:<0 or 1>" },
    { "username", "s:<string>" },
    { "screen mode id", "i:<1 or 2>" },
    { "session bpp", "i:<8, 15, 16, 24, or 32>" },
    { "use multimon", "i:<0 or 1>" },
  };

  static void Main(string[] args)
  {
    Application.EnableVisualStyles();


    // if no arguments are provided, show an error dialog
    if (args.Length == 0)
    {
      MessageBox.Show("No RDP URL provided. Please use 'rdp://<parameters>'.", "Remote Desktop Protocol Handler", MessageBoxButtons.OK, MessageBoxIcon.Error);
      return;
    }

    // if no RDP URL is provided or it does not start with "rdp://", show an error message
    if (!args[0].StartsWith("rdp://"))
    {
      MessageBox.Show("Invalid RDP URL format. Please use 'rdp://<parameters>'.", "Remote Desktop Protocol Handler", MessageBoxButtons.OK, MessageBoxIcon.Error);
      return;
    }

    string query = args[0].Replace("rdp://", "");
    var queryParams = HttpUtility.ParseQueryString(query);

    // Initialize an empty RDP config dictionary
    Dictionary<string, string> rdpConfig = new Dictionary<string, string>();

    // Process URL parameters
    foreach (string key in queryParams.Keys)
    {
      if (key != null)
      {
        string lowerKey = key.ToLower();
        if (rdpOptions.ContainsKey(lowerKey))
        {
          rdpConfig[lowerKey] = queryParams[key];
        }
      }
    }

    // Generate RDP content only with provided parameters
    string rdpContent = "";
    foreach (var entry in rdpConfig)
    {
      rdpContent += entry.Key + ":" + entry.Value + "\n";
    }

    // Create a temporary .rdp file
    string tempFile = Path.GetTempFileName();
    File.Delete(tempFile);
    string tempRdpFile = tempFile + ".rdp";
    File.WriteAllText(tempRdpFile, rdpContent);

    // Launch the RDP session without a console window
    ProcessStartInfo psi = new ProcessStartInfo
    {
      FileName = "mstsc.exe",
      Arguments = tempRdpFile,
      UseShellExecute = true,
      WindowStyle = ProcessWindowStyle.Hidden // Prevents console pop-up
    };
    Process.Start(psi);

    // delete the temporary file after a few seconds
    System.Threading.Thread.Sleep(5000);
    if (File.Exists(tempRdpFile))
    {
      File.Delete(tempRdpFile);
    }
    else
    {
      MessageBox.Show("Temporary RDP file not found for deletion.", "Remote Desktop Protocol Handler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
  }
}
