using System;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Settings;

internal sealed class SettingsForm : Form
{
    public SettingsForm()
    {
        Text = "Settings";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(600, 300);
        MaximizeBox = false;
        ShowInTaskbar = true;
        ControlBox = true;
        Icon = new Icon("media\\lock.ico");


    }
}