using System;
using System.Configuration;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;

namespace About;

internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(600, 300);
        MaximizeBox = false;
        ShowInTaskbar = true;
        ControlBox = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Icon = new Icon("media\\lock.ico");
    }
}