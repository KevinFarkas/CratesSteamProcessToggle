using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;

namespace CratesSteamProcessToggle
{
    public class MainForm : Form
    {
        private Button btnKillSteam;
        private Button btnStartSteam;
        private Label lblStatusTitle;
        private Label lblStatusValue;
        private Panel statusPanel;

        private Label lblProcessesTitle;
        private ListBox lstProcesses;
        private Panel processesPanel;

        private Timer refreshTimer;

        private readonly string[] steamProcessNames =
        {
            "steam",
            "steamwebhelper",
            "SteamService",
            "gameoverlayui"
        };

        public MainForm()
        {
            InitializeComponents();
            RefreshSteamProcessList();

            refreshTimer = new Timer();
            refreshTimer.Interval = 3000; // 3 seconds
            refreshTimer.Tick += (s, e) => RefreshSteamProcessList();
            refreshTimer.Start();
        }

        private void InitializeComponents()
        {
            // Form
            Text = "Crates Steam Toggle";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 700;
            Height = 260;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            // Dark theme
            BackColor = Color.FromArgb(25, 25, 25);

            // Buttons
            btnKillSteam = new Button
            {
                Text = "Kill Steam",
                Left = 20,
                Top = 20,
                Width = 150,
                Height = 40,
                FlatStyle = FlatStyle.Flat
            };
            btnKillSteam.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnKillSteam.FlatAppearance.BorderSize = 1;
            btnKillSteam.BackColor = Color.FromArgb(45, 45, 45);
            btnKillSteam.ForeColor = Color.White;
            btnKillSteam.Click += BtnKillSteam_Click;

            btnStartSteam = new Button
            {
                Text = "Start Steam",
                Left = 190,
                Top = 20,
                Width = 150,
                Height = 40,
                FlatStyle = FlatStyle.Flat
            };
            btnStartSteam.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnStartSteam.FlatAppearance.BorderSize = 1;
            btnStartSteam.BackColor = Color.FromArgb(45, 45, 45);
            btnStartSteam.ForeColor = Color.White;
            btnStartSteam.Click += BtnStartSteam_Click;

            // Status panel
            statusPanel = new Panel
            {
                Left = 20,
                Top = 80,
                Width = 320,
                Height = 130,
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblStatusTitle = new Label
            {
                Text = "Status",
                Left = 10,
                Top = 10,
                Width = 300,
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            lblStatusValue = new Label
            {
                Text = "Idle",
                Left = 10,
                Top = 40,
                Width = 300,
                Height = 70,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

            statusPanel.Controls.Add(lblStatusTitle);
            statusPanel.Controls.Add(lblStatusValue);

            // Processes panel
            processesPanel = new Panel
            {
                Left = 360,
                Top = 20,
                Width = 300,
                Height = 190,
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblProcessesTitle = new Label
            {
                Text = "Running Steam Processes",
                Left = 10,
                Top = 10,
                Width = 280,
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            lstProcesses = new ListBox
            {
                Left = 10,
                Top = 35,
                Width = 280,
                Height = 135,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9F, FontStyle.Regular)
            };

            processesPanel.Controls.Add(lblProcessesTitle);
            processesPanel.Controls.Add(lstProcesses);

            // Add controls to form
            Controls.Add(btnKillSteam);
            Controls.Add(btnStartSteam);
            Controls.Add(statusPanel);
            Controls.Add(processesPanel);
        }

        private void BtnKillSteam_Click(object sender, EventArgs e)
        {
            int killedCount = 0;

            foreach (string name in steamProcessNames)
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
                            // Ignore failures (permissions, etc.)
                        }
                    }
                }
                catch
                {
                    // Ignore GetProcessesByName failures
                }
            }

            lblStatusValue.Text = "Steam processes ended";
            RefreshSteamProcessList();
        }

        private void BtnStartSteam_Click(object sender, EventArgs e)
        {
            try
            {
                string steamExe = GetSteamExePath();

                if (!string.IsNullOrEmpty(steamExe) && File.Exists(steamExe))
                {
                    Process.Start(steamExe);
                    lblStatusValue.Text = "Steam started";
                }
                else
                {
                    lblStatusValue.Text = "Steam.exe not found";
                }
            }
            catch (Exception ex)
            {
                lblStatusValue.Text = "Error starting Steam: " + ex.Message;
            }

            RefreshSteamProcessList();
        }

        private void RefreshSteamProcessList()
        {
            lstProcesses.Items.Clear();

            foreach (string name in steamProcessNames)
            {
                try
                {
                    Process[] procs = Process.GetProcessesByName(name);
                    foreach (var proc in procs)
                    {
                        string display = $"{proc.ProcessName}.exe (PID {proc.Id})";
                        lstProcesses.Items.Add(display);
                    }
                }
                catch
                {
                    // ignore
                }
            }

            if (lstProcesses.Items.Count == 0)
            {
                lstProcesses.Items.Add("<No Steam processes running>");
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
