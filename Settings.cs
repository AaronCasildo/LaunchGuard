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

        DataGridView settingsGrid = new DataGridView()
        {
            Location = new Point(20, 20),
            Size = new Size(560, 180),
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            ReadOnly = false,
            ColumnCount = 2
        };  
        Controls.Add(settingsGrid);

        settingsGrid.Columns[0].Name = "Software process name";
        settingsGrid.Columns[1].Name = "Password";
        settingsGrid.Columns[0].Width = 260;
        settingsGrid.Columns[1].Width = 255;
    }
}