using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace About;

internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About LaunchGuard";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(400, 280);
        MaximizeBox = false;
        ShowInTaskbar = false;
        ControlBox = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Icon = new Icon("media\\lock.ico");

        BuildUI();
    }

    private void BuildUI()
    {
        // --- Logo ---
        var logo = new PictureBox
        {
            Image = Image.FromFile("media\\lock.ico"),
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(48, 48),
            Location = new Point(20, 20)
        };

        // --- App name + version ---
        var appName = new Label
        {
            Text = "LaunchGuard",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(78, 20)
        };

        var version = new Label
        {
            Text = "Version 1.0.0",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(80, 46)
        };

        var ossBadge = new Label
        {
            Text = "⬤  Open Source — MIT License",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(42, 122, 42),
            BackColor = Color.FromArgb(232, 245, 232),
            AutoSize = true,
            Padding = new Padding(6, 3, 6, 3),
            Location = new Point(79, 68)
        };


        Controls.AddRange(new Control[]
        {
            logo, appName, version, ossBadge,
        });
    }
}