using System;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Management;

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
            MessageBox.Show("System initialized:" + process["Name"], "LaunchGuard", 
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        watcher.Start(); //duh, 5 min wondering why it didn't work hahahh
        
    }
}