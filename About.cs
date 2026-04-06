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

        // --- Divider ---
        var divider = new Panel
        {
            BackColor = Color.FromArgb(200, 200, 200),
            Size = new Size(360, 1),
            Location = new Point(20, 104)
        };

        // --- Description ---
        var desc = new Label
        {
             Text = "LaunchGuard is a Windows process-locking utility that gates application " +
                 "access behind Windows credential authentication. It monitors running " +
                 "processes and prevents unauthorized use of protected apps using " +
                 "WMI-based interception and full process-tree termination.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(51, 51, 51),
             AutoSize = true,
             MaximumSize = new Size(360, 0),
            Location = new Point(20, 114)
        };

        // --- Author row ---
        var authorLabel = new Label
        {
            Text = "Author",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(20, 200)
        };

        var authorValue = new Label
        {
            Text = "Aaron Casildo",
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Location = new Point(90, 200)
        };

        // --- GitHub link ---
        var sourceLabel = new Label
        {
            Text = "Source",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(20, 220)
        };

        var githubLink = new LinkLabel
        {
            Text = "github.com/AaronCasildo/LaunchGuard",
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Location = new Point(90, 220)
        };
        githubLink.LinkClicked += (_, _) =>
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/AaronCasildo/LaunchGuard",
                UseShellExecute = true
            });

        // --- OK button ---
        var btnOk = new Button
        {
            Text = "OK",
            Size = new Size(80, 26),
            Location = new Point(300, 244),
            DialogResult = DialogResult.OK
        };
        btnOk.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            logo, appName, version, ossBadge,
            divider, desc,
            authorLabel, authorValue,
            sourceLabel, githubLink,
            btnOk
        });
    }
}