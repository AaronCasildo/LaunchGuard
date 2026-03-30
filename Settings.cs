using System;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Settings;

internal sealed class SettingsForm : Form
{
    private DataGridView settingsGrid;
    public SettingsForm()
    {
        Text = "Settings";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(600, 300);
        MaximizeBox = false;
        ShowInTaskbar = true;
        ControlBox = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Icon = new Icon("media\\lock.ico");

        Button SaveButton = new Button()
        {
            Text = "Save",
            Location = new Point(500, 260),
            Size = new Size(80, 30)
        };  
        Controls.Add(SaveButton);
        SaveButton.Click += SaveButton_Click;
        
        settingsGrid = new DataGridView()
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

        foreach (var entry in LaunchGuard.AppConfig.LockedProcesses)
        {
            settingsGrid.Rows.Add(entry.Key, entry.Value);
        }
    }
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        //Tmw is today!
        LaunchGuard.AppConfig.LockedProcesses.Clear();
        foreach (DataGridViewRow row in settingsGrid.Rows)
        {
            if (row.IsNewRow) continue; // Skip the new row placeholder
            
            string? process = row.Cells[0].Value?.ToString()?.Trim();
            string? password = row.Cells[1].Value?.ToString()?.Trim();

            if (!string.IsNullOrEmpty(process) && !string.IsNullOrEmpty(password))
            {
                LaunchGuard.AppConfig.LockedProcesses[process] = password;
            }
        }
        MessageBox.Show(
            "Settings saved successfully.",
            "Success",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}