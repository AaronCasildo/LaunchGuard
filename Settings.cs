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
        Icon = new Icon("media\\lock.ico");
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(600, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
    }
}