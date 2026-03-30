using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Management;
using Microsoft.VisualBasic.ApplicationServices;
using System.Windows.Forms.VisualStyles;

namespace LaunchGuard;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
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

internal sealed class MainForm : Form
{
    public MainForm()
    {
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
            Location = new Point(20, 20)
        };
        Controls.Add(welcomeLabel);

        ListView processListView = new ListView()
        {
            Location = new Point(20, 60),
            Size = new Size(600, 200),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };  
        processListView.Columns.Add("Process", 200);
        processListView.Columns.Add("Started", 160);
        processListView.Columns.Add("PID", 80);

        processListView.Columns[0].Width = 300;
        processListView.Columns[1].Width = 160;
        processListView.Columns[2].Width = processListView.ClientSize.Width - 480;
        Controls.Add(processListView);

        Button settingsButton = new Button()
        {
            Text = "Settings",
            Location = new Point(20, 280),
            Size = new Size(100, 30)
        };  
        Controls.Add(settingsButton);
        settingsButton.Click += settingsButton_Click;
        
        //Software initialization watcher
        var query = new WqlEventQuery(
            "__InstanceCreationEvent",
            new TimeSpan(0,0,1), //1 second polling interval
            "TargetInstance ISA 'Win32_Process'"
        );

        var watcher = new ManagementEventWatcher(query);
        watcher.EventArrived += (sender,e) =>
        {
            var process = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var processName = process["Name"]?.ToString() ?? "Unknown";

            BeginInvoke(() =>
            {
                newEvent(processName, process["ProcessId"]?.ToString() ?? "N/A");
            });
        };

        void newEvent(string processName, string pid)
        {
            var item = new ListViewItem(processName);
            item.SubItems.Add(DateTime.Now.ToString());
            item.SubItems.Add(pid);
            processListView.Items.Add(item);

            if (AppConfig.LockedProcesses.TryGetValue(processName, out string? requiredPassword))
            {
                //Note: Kill() will fail if the process has admin privileges, add admin exec (WIP)
                try
                {
                    int processId = int.Parse(pid);
                    var process = System.Diagnostics.Process.GetProcessById(processId);
                    string execPath = process.MainModule?.FileName ?? string.Empty;
                    process.Kill(); // Kill immediately, one tap headshot
                    
                    BeginInvoke(() =>
                    {
                        if(uservalidation(processName, requiredPassword))
                        {
                            if (!string.IsNullOrEmpty(execPath))
                            {
                                System.Diagnostics.Process.Start(execPath);
                            }
                        }
                    });
                }
                catch (ArgumentException)
                {
                    // Process exist before password was set, ignore
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to kill process {processName} (PID: {pid}): {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }

            bool uservalidation(string processName, string requiredPassword)
            {
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Enter password to allow {processName} to run:",
                    "Authentication Required",
                    "",
                    -1, -1
                );

                if (input == requiredPassword)
                {
                    return true;
                }
                else
                {
                    MessageBox.Show(
                        $"Incorrect password. {processName} will remain blocked.",
                        "Access Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return false;
                }
            }
        }

        watcher.Start(); //duh, 5 min wondering why it didn't work hahahh
        
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
}