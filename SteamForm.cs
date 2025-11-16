using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CratesSteamProcessToggle
{
    public class MainForm : Form
    {
        private Button btnKillSteam;
        private Button btnStartSteam;
        private Label lblStatus;

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Crates Steam Process Toggle";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 400;
            this.Height = 200;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            btnKillSteam = new Button
            {
                Text = "Kill Steam",
                Left = 30,
                Top = 30,
                Width = 150,
                Height = 40
            };
            btnKillSteam.Click += BtnKillSteam_Click;

            btnStartSteam = new Button
            {
                Text = "Start Steam",
                Left = 210,
                Top = 30,
                Width = 150,
                Height = 40
            };
            btnStartSteam.Click += BtnStartSteam_Click;

            lblStatus = new Label
            {
                Text = "Status: Idle",
                Left = 30,
                Top = 100,
                Width = 330,
                Height = 40,
                AutoSize = false
            };

            Controls.Add(btnKillSteam);
            Controls.Add(btnStartSteam);
            Controls.Add(lblStatus);
        }

        private void BtnKillSteam_Click(object sender, EventArgs e)
        {
            string[] processNames =
            {
                "steam",
                "steamwebhelper",
                "SteamService",
                "gameoverlayui"
            };

            int killedCount = 0;

            foreach (string name in processNames)
            {
                try
                {
                    Process[] procs = Process.GetProcessesByName(name);
                    foreach (var proc in procs)
                    {
                        try
                        {
                            proc.Kill();
                            proc.WaitForExit(5000);
                            killedCount++;
                        }
                        catch
                        {
                            // Ignore processes we can't kill (permissions, etc.)
                        }
                    }
                }
                catch
                {
                    // Ignore failures on GetProcessesByName
                }
            }

            lblStatus.Text = "Steam processes ended";
        }

        private void BtnStartSteam_Click(object sender, EventArgs e)
        {
            try
            {
                string steamExe = GetSteamExePath();

                if (!string.IsNullOrEmpty(steamExe) && File.Exists(steamExe))
                {
                    Process.Start(steamExe);
                    lblStatus.Text = "Steam started";
                }
                else
                {
                    lblStatus.Text = "Steam.exe not found";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error starting Steam: " + ex.Message;
            }
        }

        private string GetSteamExePath()
        {
            // 1. HKCU\Software\Valve\Steam
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string path = key.GetValue("SteamPath") as string
                                      ?? key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            string exe = Path.Combine(path, "steam.exe");
                            if (File.Exists(exe))
                                return exe;
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }

            // 2. HKLM\SOFTWARE\WOW6432Node\Valve\Steam
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string path = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            string exe = Path.Combine(path, "steam.exe");
                            if (File.Exists(exe))
                                return exe;
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }

            // 3. Common default paths
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            string candidate1 = Path.Combine(pf86, "Steam", "steam.exe");
            if (File.Exists(candidate1))
                return candidate1;

            string candidate2 = Path.Combine(pf, "Steam", "steam.exe");
            if (File.Exists(candidate2))
                return candidate2;

            return null;
        }
    }
}
