using System;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Management;
using System.Drawing.Text;

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

        ListBox processListBox = new ListBox()
        {
            Location = new Point(20, 60),
            Size = new Size(600, 200),
        };  
        Controls.Add(processListBox);

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
                newEvent($"System initialized: {processName}");
            });
        };

        void newEvent(string message)
        {
            processListBox.Items.Insert(0, message);
            processListBox.SelectedIndex = 0;
        }

        watcher.Start(); //duh, 5 min wondering why it didn't work hahahh
        
    }
}