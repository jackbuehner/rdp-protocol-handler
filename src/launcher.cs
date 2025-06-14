using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Web;
using System.Windows.Forms;
using Microsoft.Win32;

class RdpLauncher
{
  /// <summary>
  /// The dictionary of valid RDP options and their expected formats.
  /// <br />
  /// See https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/remote-desktop-uri
  /// and https://learn.microsoft.com/en-us/azure/virtual-desktop/rdp-properties.
  /// </summary>
  static Dictionary<string, string> rdpOptions = new Dictionary<string, string>
  {
    { "alternate full address", "s:<value>" },
    { "authentication level", "i:<value>" },
    { "autoreconnect max retries", "i:<value>" },
    { "autoreconnection enabled", "i:<value>" },
    { "bandwidthautodetect", "i:<value>" },
    { "compression", "i:<value>" },
    { "connection type", "i:<value>" },
    { "domain", "s:<value>" },
    { "enablecredsspsupport", "i:<value>" },
    { "enablerdsaadauth", "i:<value>" },
    { "full address", "s:<value>" },
    { "kdcproxyname", "s:<value>" },
    { "negotiate security layer", "i:<value>" },
    { "networkautodetect", "i:<value>" },
    { "password 51", "b:<value>" },
    { "prompt for credentials", "i:<value>" },
    { "prompt for credentials on client", "i:<value>" },
    { "promptcredentialonce", "i:<value>" },
    { "server port", "i:<value>" },
    { "targetisaadjoined", "i:<value>" },
    { "username", "s:<value>" },
    { "allow desktop composition", "i:<value>" },
    { "allow font smoothing", "i:<value>" },
    { "bitmapcachepersistenable", "i:<value>" },
    { "bitmapcachesize", "i:<value>" },
    { "desktopheight", "i:<value>" },
    { "desktop size id", "i:<value>" },
    { "desktopscalefactor", "i:<value>" },
    { "desktopwidth", "i:<value>" },
    { "disable full window drag", "i:<value>" },
    { "disable menu anims", "i:<value>" },
    { "disable themes", "i:<value>" },
    { "disable wallpaper", "i:<value>" },
    { "dynamic resolution", "i:<value>" },
    { "enablesuperpan", "i:<value>" },
    { "encode redirected video capture", "i:<value>" },
    { "maximizetocurrentdisplays", "i:<value>" },
    { "redirectdirectx", "i:<value>" },
    { "screen mode id", "i:<value>" },
    { "selectedmonitors", "s:<value>" },
    { "session bpp", "i:<value>" },
    { "singlemoninwindowedmode", "i:<value>" },
    { "smart sizing", "i:<value>" },
    { "span monitors", "i:<value>" },
    { "superpanaccelerationfactor", "i:<value>" },
    { "use multimon", "i:<value>" },
    { "videoplaybackmode", "i:<value>" },
    { "winposstr", "s:<value>" },
    { "gatewaycredentialssource", "i:<value>" },
    { "gatewayhostname", "s:<value>" },
    { "gatewayprofileusagemethod", "i:<value>" },
    { "gatewayusagemethod", "i:<value>" },
    { "audiocapturemode", "i:<value>" },
    { "audiomode", "i:<value>" },
    { "audioqualitymode", "i:<value>" },
    { "camerastoredirect", "s:<value>" },
    { "devicestoredirect", "s:<value>" },
    { "drivestoredirect", "s:<value>" },
    { "redirected video capture encoding quality", "i:<value>" },
    { "redirectcomports", "i:<value>" },
    { "redirectlocation", "i:<value>" },
    { "redirectposdevices", "i:<value>" },
    { "redirectprinters", "i:<value>" },
    { "redirectsmartcards", "i:<value>" },
    { "usbdevicestoredirect", "s:<value>" },
    { "disableremoteappcapscheck", "i:<value>" },
    { "remoteapplicationcmdline", "s:<value>" },
    { "remoteapplicationfile", "s:<value>" },
    { "remoteapplicationexpandcmdline", "i:<value>" },
    { "remoteapplicationexpandworkingdir", "i:<value>" },
    { "remoteapplicationicon", "s:<value>" },
    { "remoteapplicationmode", "i:<value>" },
    { "remoteapplicationname", "s:<value>" },
    { "remoteapplicationprogram", "s:<value>" },
    { "workspaceid", "s:<value>" },
    { "administrative session", "i:<value>" },
    { "alternate shell", "s:<value>" },
    { "disable ctrl+alt+del", "i:<value>" },
    { "disableconnectionsharing", "i:<value>" },
    { "displayconnectionbar", "i:<value>" },
    { "keyboardhook", "i:<value>" },
    { "pinconnectionbar", "i:<value>" },
    { "public mode", "i:<value>" },
    { "redirectclipboard", "i:<value>" },
    { "redirectwebauthn", "i:<value>" },
    { "shell working directory", "s:<value>" },
    { "signscope", "s:<value>" }
  };

  static void Main(string[] args)
  {
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);


    // if no arguments are provided, show an error dialog
    if (args.Length == 0)
    {
      Application.Run(new RdpProtocolHandlerGuide.MainForm());
      return;
    }

    // if no RDP URL is provided or it does not start with "rdp://", show an error message
    if (!args[0].StartsWith("rdp://"))
    {
      MessageBox.Show("Invalid RDP URL format. Please use 'rdp://<parameters>'.", "Remote Desktop Protocol Handler", MessageBoxButtons.OK, MessageBoxIcon.Error);
      return;
    }

    string query = args[0].Replace("rdp://", "").TrimEnd('/');
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


namespace RdpProtocolHandlerGuide
{
  public partial class MainForm : Form
  {
    public MainForm()
    {
      this.AutoScaleMode = AutoScaleMode.Dpi;
      SetupUI();
    }

    private bool IsDarkModeActive()
    {
      try
      {
        // From register
        int theme = (int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", -1);
        return theme == 0;    // dark mode
      }
      catch { return false; } //In case of error
    }

    private void SetupUI()
    {
      bool isDarkMode = IsDarkModeActive();

      // Main form setup
      this.Text = "Remote Desktop Protocol Handler";
      this.ClientSize = new Size(480, 520);
      if (isDarkMode)
      {
        this.BackColor = Color.FromArgb(66, 66, 66);
        this.ForeColor = Color.FromArgb(220, 220, 220);
      }
      else
      {
        this.BackColor = Color.FromArgb(193, 193, 193);
        this.ForeColor = Color.FromArgb(26, 26, 26);
      }
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.None;
      this.ShowInTaskbar = false;
      this.Padding = new Padding(1);
      this.Margin = new Padding(0);
      RoundControlCorners(this, 8);

      // close the form when the user clicks outside of it
      this.Deactivate += (sender, e) =>
      {
        if (!this.Focused)
        {
          this.Close();
        }
      };

      // Create main container
      var mainPanel = new Panel
      {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        Padding = new Padding(16),
        BackColor = Color.White,
      };
      if (isDarkMode)
      {
        mainPanel.BackColor = Color.FromArgb(43, 43, 43);
      }
      mainPanel.ClientSize = new Size(478, 518);
      RoundControlCorners(mainPanel, 8);
      this.Controls.Add(mainPanel);

      // Title
      var titleLabel = new Label
      {
        Text = "Get started with RDP Protocol Handler",
        Font = new Font("Segoe UI Semibold", 20, GraphicsUnit.Pixel),
        Height = 20,
        ForeColor = Color.FromArgb(26, 26, 26),
        AutoSize = true,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 6, 0, 14)
      };
      if (isDarkMode)
      {
        titleLabel.ForeColor = Color.White;
      }

      // Introduction
      var introLabel = new Label
      {
        Text = "Your Windows device can now launch remote desktop sessions via rdp://\n" +
              "URLs. Quickly connect to remote desktops by simply clicking or entering\n" +
              "specially-formatted rdp:// links.",
        Font = new Font("Segoe UI", 14, GraphicsUnit.Pixel),
        Height = (20 * 3),
        AutoSize = true,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 0, 0, 20)
      };

      // URL Construction section
      var constructHeader = CreateSectionHeader("How to Construct an rdp:// URL");

      var formatLabel = new Label
      {
        Text = "The rdp:// URL follows this format:",
        Font = new Font("Segoe UI", 10),
        AutoSize = true,
        Dock = DockStyle.Top,
        Margin = new Padding(0, 0, 0, 10)
      };

      var formatBox = new TextBox
      {
        Text = "rdp://full%20address=s:<PC_NAME_OR_IP>:<PORT>&<PARAM1>=s:<VALUE1>&<PARAM2>=i:<VALUE2>",
        Font = new Font("Consolas", 10),
        ReadOnly = true,
        BackColor = Color.AliceBlue,
        BorderStyle = BorderStyle.FixedSingle,
        Dock = DockStyle.Top,
        Margin = new Padding(0, 0, 0, 20),
        Padding = new Padding(10)
      };
      if (isDarkMode)
      {
        formatBox.BackColor = Color.FromArgb(50, 50, 50);
        formatBox.ForeColor = Color.White;
      }

      // Tip label
      var tipLabel = new Label
      {
        Text = "Note: Use %20 for spaces in parameter names and values.",
        Font = new Font("Segoe UI", 9, FontStyle.Italic),
        AutoSize = true,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 0, 0, 20)
      };

      // Testing section
      var testHeader = CreateSectionHeader("Use a test rdp:// URL");

      var testLabel = new Label
      {
        Text = "1. Open Run (Win + X, choose Run)\n2. Copy this test link:\n3. Paste into Run\n4. Press OK → Watch Remote Desktop launch!",
        Font = new Font("Segoe UI", 10),
        AutoSize = true,
        Dock = DockStyle.Top,
        Margin = new Padding(0, 0, 0, 10)
      };

      var testBox = new TextBox
      {
        Text = "rdp://full%20address=s:localhost:3389&username=s:test&disable%20wallpaper=i:1",
        Font = new Font("Consolas", 10),
        ReadOnly = false,
        BackColor = Color.AliceBlue,
        BorderStyle = BorderStyle.FixedSingle,
        Dock = DockStyle.Top,
        Margin = new Padding(0, 0, 0, 10)
      };
      if (isDarkMode)
      {
        testBox.BackColor = Color.FromArgb(50, 50, 50);
        testBox.ForeColor = Color.White;
      }

      var noteLabel = new Label
      {
        Text = "Note: Replace \"localhost\" with your actual PC name/IP to test properly.",
        Font = new Font("Segoe UI", 9, FontStyle.Italic),
        AutoSize = true,
        Dock = DockStyle.Top,
        Margin = new Padding(0, 0, 0, 30)
      };

      // Footer for holding buttons
      var footer = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 80,
        BackColor = Color.FromArgb(243, 243, 243),
        Padding = new Padding(24)
      };
      if (isDarkMode)
      {
        footer.BackColor = Color.FromArgb(32, 32, 32);
      }
      this.Controls.Add(footer);

      // Test button
      var testButton = new Button
      {
        Text = "Try Test URL Now",
        Font = new Font("Segoe UI", 10, FontStyle.Regular),
        BackColor = Color.FromArgb(0, 102, 204),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Dock = DockStyle.Left,
        Width = 200,
        Height = 32,
        Margin = new Padding(0, 24, 0, 24),
        FlatAppearance = {
          BorderSize = 0,
          MouseOverBackColor = Color.FromArgb(0, 82, 164),
          MouseDownBackColor = Color.FromArgb(0, 61, 122)
        },
        Padding = new Padding(12, 0, 12, 0),
      };
      // if (isDarkMode)
      // {
      //   testButton.BackColor = Color.FromArgb(0, 102, 204);
      //   testButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 82, 164);
      //   testButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 61, 122);
      // }
      RoundControlCorners(testButton, 4);
      testButton.Click += (sender, e) =>
      {
        try
        {
          Process.Start(testBox.Text);
        }
        catch (Exception ex)
        {
          MessageBox.Show("Failed to launch RDP URL:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      };

      // close button
      var closeButton = new BorderedButton
      {
        Text = "Close",
        Font = new Font("Segoe UI", 10, FontStyle.Regular),
        BackColor = Color.FromArgb(255, 255, 255),
        ForeColor = Color.FromArgb(26, 26, 26),
        FlatStyle = FlatStyle.Flat,
        Dock = DockStyle.Right,
        Width = 200,
        Height = 32,
        BorderColor = Color.FromArgb(229, 229, 229),
        BorderSize = 1,
        Margin = new Padding(0, 24, 0, 24),
        FlatAppearance = {
          BorderSize = 0,
          MouseOverBackColor = Color.FromArgb(246, 246, 246),
          MouseDownBackColor = Color.FromArgb(245, 245, 245)
        },
        Padding = new Padding(12, 0, 12, 0),
      };
      if (isDarkMode)
      {
        closeButton.BackColor = Color.FromArgb(45, 45, 45);
        closeButton.ForeColor = Color.White;
        closeButton.BorderColor = Color.FromArgb(48, 48, 48);
        closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
        closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(39, 39, 39);
      }
      RoundControlCorners(closeButton, 4);
      closeButton.Click += (sender, e) => this.Close();

      // add controls to the footer
      footer.Controls.Add(testButton);
      footer.Controls.Add(closeButton);

      // add all controls to the main panel
      mainPanel.Controls.Add(noteLabel);
      mainPanel.Controls.Add(testBox);
      mainPanel.Controls.Add(testLabel);
      mainPanel.Controls.Add(testHeader);
      mainPanel.Controls.Add(tipLabel);
      mainPanel.Controls.Add(formatBox);
      mainPanel.Controls.Add(formatLabel);
      mainPanel.Controls.Add(constructHeader);
      mainPanel.Controls.Add(introLabel);
      mainPanel.Controls.Add(titleLabel);
    }

    private Label CreateSectionHeader(string text)
    {
      var label = new Label
      {
        Text = text,
        Font = new Font("Segoe UI Semibold", 16, GraphicsUnit.Pixel),
        Height = 24,
        ForeColor = Color.FromArgb(26, 26, 26),
        AutoSize = true,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 0, 0, 8)
      };
      if (IsDarkModeActive())
      {
        label.ForeColor = Color.White;
      }
      return label;
    }

    public static void RoundControlCorners(Control control, int _radius)
    {
      int radius = _radius * 2;
      GraphicsPath path = new GraphicsPath();
      path.AddArc(0, 0, radius, radius, 180, 90);
      path.AddArc(control.Width - radius, 0, radius, radius, 270, 90);
      path.AddArc(control.Width - radius, control.Height - radius, radius, radius, 0, 90);
      path.AddArc(0, control.Height - radius, radius, radius, 90, 90);
      path.CloseAllFigures();
      control.Region = new Region(path);
    }

    public class BorderedButton : Button
    {
      public Color BorderColor { get; set; }
      public int BorderSize { get; set; }

      protected override void OnPaint(PaintEventArgs e)
      {
        base.OnPaint(e);

        // Draw border
        if (BorderSize > 0)
        {
          Rectangle rect = ClientRectangle;
          rect.Inflate(-BorderSize, -BorderSize);

          using (Pen pen = new Pen(BorderColor, BorderSize))
          {
            pen.Alignment = PenAlignment.Inset;  // Prevent clipping
            e.Graphics.DrawRectangle(pen, rect);
          }
        }
      }
    }
  }
}
