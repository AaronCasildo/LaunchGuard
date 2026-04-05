using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;
using System.Collections.Generic;

namespace LaunchGuard;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();
        AppConfig.Load();
        Application.Run(new MainForm());
    }
}
internal static class WindowsCredentialHelper
{
    [DllImport("credui.dll", CharSet = CharSet.Unicode)]
    private static extern uint CredUIPromptForWindowsCredentials(
        ref CREDUI_INFO pUiInfo,
        uint dwAuthError,
        ref uint pulAuthPackage,
        IntPtr pvInAuthBuffer,
        uint ulInAuthBufferSize,
        out IntPtr ppvOutAuthBuffer,
        out uint pulOutAuthBufferSize,
        ref bool pfSave,
        CREDUIWIN_FLAGS dwFlags
    );

    [DllImport("credui.dll", CharSet = CharSet.Unicode)]
    private static extern bool CredUnPackAuthenticationBuffer(
        uint dwFlags,
        IntPtr pAuthBuffer,
        uint cbAuthBuffer,
        StringBuilder pszUserName,
        ref uint pcchMaxUserName,
        StringBuilder pszDomainName,
        ref uint pcchMaxDomainname,
        StringBuilder pszPassword,
        ref uint pcchMaxPassword
    );

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken
    );

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(IntPtr ptr);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDUI_INFO
    {
        public int cbSize;
        public IntPtr hwndParent;
        public string pszMessageText;
        public string pszCaptionText;
        public IntPtr hbmBanner;
    }

    [Flags]
    private enum CREDUIWIN_FLAGS : uint
    {
        CREDUIWIN_GENERIC              = 0x1,
        CREDUIWIN_ENUMERATE_CURRENT_USER = 0x200,
    }

    /// <summary>
    /// Shows the native Windows credential prompt and validates the entered password.
    /// Returns true if the user authenticated successfully.
    /// </summary>
    public static bool PromptAndValidate(IntPtr ownerHandle)
    {
        var info = new CREDUI_INFO
        {
            cbSize         = Marshal.SizeOf(typeof(CREDUI_INFO)),
            hwndParent     = ownerHandle,
            pszCaptionText = "Authentication Required",
            pszMessageText = "Enter your Windows credentials to access Settings."
        };

        uint authPackage = 0;
        bool save        = false;

        uint result = CredUIPromptForWindowsCredentials(
            ref info, 0, ref authPackage,
            IntPtr.Zero, 0,
            out IntPtr outBuffer, out uint outBufferSize,
            ref save,
            CREDUIWIN_FLAGS.CREDUIWIN_ENUMERATE_CURRENT_USER
        );

        if (result != 0) return false; // User cancelled

        var usernameSb = new StringBuilder(256);
        var domainSb   = new StringBuilder(256);
        var passwordSb = new StringBuilder(256);
        uint unLen = 256, dnLen = 256, pwLen = 256;

        CredUnPackAuthenticationBuffer(
            0, outBuffer, outBufferSize,
            usernameSb, ref unLen,
            domainSb,   ref dnLen,
            passwordSb, ref pwLen
        );

        CoTaskMemFree(outBuffer);

        string username = usernameSb.ToString();
        string password = passwordSb.ToString();
        string domain   = "."; // local machine by default

        // Strip domain prefix if present
        if (username.Contains('\\'))
        {
            var parts = username.Split('\\', 2);
            domain   = parts[0];
            username = parts[1];
        }

        bool valid = LogonUser(
            username, domain, password,
            2,  // LOGON32_LOGON_INTERACTIVE
            0,  // LOGON32_PROVIDER_DEFAULT
            out IntPtr token
        );

        if (valid) CloseHandle(token);
        return valid;
    }
}

internal static class ProcessTreeKiller
{
    public static string KillTree(int rootPid)
    {
        string execPath = string.Empty;
        try
        {
            var root = Process.GetProcessById(rootPid);
            execPath = root.MainModule?.FileName ?? string.Empty;
        }
        catch { }

        var children = new Dictionary<int, List<int>>();
        using var searcher = new ManagementObjectSearcher(
            "SELECT ProcessId, ParentProcessId FROM Win32_Process");

        foreach (ManagementObject obj in searcher.Get())
        {
            int pid = Convert.ToInt32(obj["ProcessId"]);
            int parent = Convert.ToInt32(obj["ParentProcessId"]);

            if (!children.ContainsKey(parent))
                children[parent] = new List<int>();
            children[parent].Add(pid);
        }

        KillSubtree(rootPid, children);
        return execPath;
    }

    private static void KillSubtree(int pid, Dictionary<int, List<int>> children)
    {
        if (children.TryGetValue(pid, out var kids))
            foreach (int child in kids)
                KillSubtree(child, children);

        try { Process.GetProcessById(pid).Kill(); }
        catch { }
    }
}

internal sealed class MainForm : Form
{
    private readonly Dictionary<string, DateTime> approvedUntil = new();
    private readonly TimeSpan approvalWindow = TimeSpan.FromSeconds(5);

    private readonly HashSet<string> interceptionsInFlight = new();
    private readonly Button activateDefensesButton;
    private readonly PictureBox protectionStatusIcon;
    private readonly Label protectionStatusLabel;
    private readonly Image? protectedShieldImage;
    private readonly Image? unprotectedShieldImage;
    private bool defensesActive = true;

    public MainForm()
    {
        defensesActive = AppConfig.LoadDefensesActiveState(defaultValue: true);

        // Form properties
        Text = "LaunchGuard";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 360);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Icon = new Icon("media\\lock.ico");

        Label welcomeLabel = new Label()
        {
            Text = "Welcome to LaunchGuard!",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 20)
        };
        Controls.Add(welcomeLabel);

        protectedShieldImage = TryLoadImage("media\\green_shiled.png");
        unprotectedShieldImage = TryLoadImage("media\\red_shield.png");

        protectionStatusIcon = new PictureBox()
        {
            Location = new Point(580, 14),
            Size = new Size(40, 40),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        Controls.Add(protectionStatusIcon);

        protectionStatusLabel = new Label()
        {
            Location = new Point(480, 24),
            Size = new Size(96, 20),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(protectionStatusLabel);

        ListView processListView = new ListView()
        {
            Location = new Point(20, 60),
            Size = new Size(600, 200),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };  
        processListView.Columns.Add("Process", 300);
        processListView.Columns.Add("Started", 160);
        processListView.Columns.Add("PID", processListView.ClientSize.Width - 480);
        Controls.Add(processListView);

        Button settingsButton = new Button()
        {
            Text = "Settings",
            Location = new Point(20, 280),
            Size = new Size(100, 30)
        };  
        Controls.Add(settingsButton);
        settingsButton.Click += settingsButton_Click;

        activateDefensesButton = new Button()
        {
            Location = new Point(130, 280),
            Size = new Size(120, 30),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
        };  
        activateDefensesButton.FlatAppearance.BorderSize = 1;
        Controls.Add(activateDefensesButton);
        activateDefensesButton.Click += ActivateDefensesButton_Click;
        UpdateGuardButtonAppearance();
        
        
        //Software initialization watcher
        var query = new WqlEventQuery(
            "__InstanceCreationEvent",
            new TimeSpan(0,0,1), //1 second polling interval
            "TargetInstance ISA 'Win32_Process'"
        );

        var watcher = new ManagementEventWatcher(query);
        watcher.EventArrived += (sender,e) =>
        {
            var proc = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            string procName = proc["Name"]?.ToString() ?? "Unknown";
            string pidStr = proc["ProcessId"]?.ToString() ?? "0";

            BeginInvoke(() => HandleNewProcess(procName, pidStr, processListView));
        };

        watcher.Start();
    }

    private void HandleNewProcess(string processName, string pidStr, ListView listView)
    {
        processName = processName.ToLowerInvariant();

        if (approvedUntil.TryGetValue(processName, out var until))
        {
            if (DateTime.Now < until)
                return;

            approvedUntil.Remove(processName);
        }

        var item = new ListViewItem(processName);
        item.SubItems.Add(DateTime.Now.ToString());
        item.SubItems.Add(pidStr);
        listView.Items.Add(item);

        if (!AreDefensesActive())
            return;

        if (!AppConfig.LockedProcesses.TryGetValue(processName, out string? requiredPassword))
            return;

        if (!int.TryParse(pidStr, out int pid)) return;

        // Nuke the whole thing
        if (!interceptionsInFlight.Add(processName))
        {
            try { Process.GetProcessById(pid).Kill(); } catch { }
            return;
        }

        string execPath = string.Empty;
        try
        {
            var proc = Process.GetProcessById(pid);
            execPath = proc.MainModule?.FileName ?? string.Empty;
        }
        catch { }

        string capturedExecPath = execPath;

        System.Threading.Tasks.Task.Delay(800).ContinueWith(_ =>
        {
            BeginInvoke(() =>
            {
                try
                {
                    if (!AreDefensesActive())
                        return;

                    KillAllByName(processName);

                    if (!AreDefensesActive())
                        return;

                    if (ValidatePassword(processName, requiredPassword) && !string.IsNullOrEmpty(capturedExecPath))
                    {
                        approvedUntil[processName] = DateTime.Now.Add(approvalWindow);

                        AppLauncher.Launch(capturedExecPath, processName);
                    }
                }
                finally
                {
                    interceptionsInFlight.Remove(processName);
                }
            });
        });
    }

    private bool AreDefensesActive()
    {
        return defensesActive;
    }

    private static void KillAllByName(string processName)
    {
        string name = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName[..^4]
            : processName;

        foreach (var proc in Process.GetProcessesByName(name))
        {
            try { ProcessTreeKiller.KillTree(proc.Id); }
            catch { }
        }
    }

    private static bool ValidatePassword(string processName, string requiredPassword)
    {
        string input = Microsoft.VisualBasic.Interaction.InputBox(
            $"Enter password to allow {processName} to run:",
            "Authentication Required"
        );

        if (input == requiredPassword) return true;

        MessageBox.Show(
            $"Incorrect password. {processName} will remain blocked.",
            "Access Denied",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );
        return false;
    }

    internal static class AppLauncher
    {
        private static readonly string WindowsAppsPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "WindowsApps"
            ).ToLowerInvariant();

        private static readonly Dictionary<string, string> KnownUriSchemes = new(StringComparer.OrdinalIgnoreCase)
        {
            { "spotify.exe", "spotify:" },
            { "discord.exe", "discord:" },
            { "ms-teams.exe", "msteams:" },
            { "whatsapp.exe", "whatsapp:" },
            { "slack.exe", "slack:" },
        };

        public static void Launch(string execPath, string processName)
        {
            if (execPath.ToLowerInvariant().StartsWith(WindowsAppsPath))
            {
                if (KnownUriSchemes.TryGetValue(processName, out string? uri))
                {
                    Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                    return;
                }

                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{execPath}\"")
                {
                    UseShellExecute = true
                });
                return;
            }

            Process.Start(new ProcessStartInfo(execPath)
            {
                UseShellExecute = true
            });
        }
    }

    private void settingsButton_Click(object? sender, EventArgs e)
    {
        // bool authenticated = WindowsCredentialHelper.PromptAndValidate(this.Handle);

        // if (!authenticated)
        // {
        //     MessageBox.Show(
        //         "Invalid credentials. Access denied.",
        //         "Authentication Failed",
        //         MessageBoxButtons.OK,
        //         MessageBoxIcon.Warning
        //     );
        //     return;
        // }
        
        //Developer sanity check - remove when not needed

        var settingsForm = new Settings.SettingsForm();
        settingsForm.ShowDialog();
    }

    private void ActivateDefensesButton_Click(object? sender, EventArgs e)
    {
        bool authenticated = WindowsCredentialHelper.PromptAndValidate(this.Handle);

        if (authenticated)
        {
            defensesActive = !defensesActive;

            MessageBox.Show(
                defensesActive
                    ? "LaunchGuard defenses are now active. Protected apps will require authentication to run."
                    : "LaunchGuard defenses are now inactive. Protected apps can run without LaunchGuard authentication.",
                defensesActive ? "Service Activated" : "Service Deactivated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            UpdateGuardButtonAppearance();
            AppConfig.SaveDefensesActiveState(defensesActive);
            return;
        }
        else
        {
            MessageBox.Show(
                defensesActive
                    ? "Authentication was not successful. LaunchGuard defenses remain active."
                    : "Authentication was not successful. LaunchGuard defenses remain inactive.",
                "Activation Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return;
        }
    }
    private void UpdateGuardButtonAppearance()
    {
        if (defensesActive)
        {
            activateDefensesButton.Text = "Stop Guard";
            activateDefensesButton.FlatAppearance.BorderColor = Color.FromArgb(198, 80, 80);
            protectionStatusIcon.Image = protectedShieldImage;
            protectionStatusLabel.Text = "Protected";
            protectionStatusLabel.ForeColor = Color.FromArgb(53, 123, 65);
        }
        else
        {
            activateDefensesButton.Text = "Start Guard";
            activateDefensesButton.FlatAppearance.BorderColor = Color.FromArgb(80, 160, 80);
            protectionStatusIcon.Image = unprotectedShieldImage;
            protectionStatusLabel.Text = "Unprotected";
            protectionStatusLabel.ForeColor = Color.FromArgb(178, 63, 63);
        }
    }

    private static Image? TryLoadImage(string relativePath)
    {
        if (!File.Exists(relativePath))
            return null;

        return Image.FromFile(relativePath);
    }
}