using System;
using System.Configuration;
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

        Button EraseAll = new Button()
        {
            Text = "Erase All Configurations",
            Location = new Point(310, 260),
            Size = new Size(170, 30),
        };  
        Controls.Add(EraseAll);
        EraseAll.Click += EraseAll_Click;
        
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
        settingsGrid.Columns[0].Width = 220;
        settingsGrid.Columns[1].Width = 225;

        var deleteCol = new DataGridViewButtonColumn()
        {
            Name = "Delete",
            HeaderText = "Delete",
            Text = "✕",
            UseColumnTextForButtonValue = true,
            Width = 55,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };

        settingsGrid.Columns.Add(deleteCol);
        settingsGrid.CellClick += SettingsGrid_CellClick;

        foreach (var entry in LaunchGuard.AppConfig.LockedProcesses)
        {
            settingsGrid.Rows.Add(entry.Key, entry.Value);
        }
    }
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        DialogResult result = MessageBox.Show(
            "Are you sure you want to save the current configurations? This action cannot be undone.",
            "Confirm Save",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (result == DialogResult.Yes){
            //Tmw is today!
            LaunchGuard.AppConfig.LockedProcesses.Clear();
            foreach (DataGridViewRow row in settingsGrid.Rows)
            {
                if (row.IsNewRow) continue; // Skip the new row placeholder
                
                string? process = row.Cells[0].Value?.ToString()?.Trim();
                string? password = row.Cells[1].Value?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(process) && !string.IsNullOrEmpty(password))
                {
                    if (!process.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show(
                            $"Process name '{process}' is invalid. It must end with '.exe'.",
                            "Invalid Input",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        continue;
                    }
                    LaunchGuard.AppConfig.LockedProcesses[process] = password;
                }
            }
            LaunchGuard.AppConfig.Save();

            MessageBox.Show(
            "Settings saved successfully.",
            "Success",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
            );
        }
        else{
            MessageBox.Show(
            "Settings not saved.",
            "Not confirmed",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
        }
        
        
    }

    private void SettingsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex == settingsGrid.Columns["Delete"]!.Index && e.RowIndex >= 0)
        {
            if (!settingsGrid.Rows[e.RowIndex].IsNewRow)
            {
                settingsGrid.Rows.RemoveAt(e.RowIndex);
                return;
            }
        }
        
    }

    private void EraseAll_Click(object? sender, EventArgs e)
    {
        settingsGrid.Rows.Clear();
    }
}