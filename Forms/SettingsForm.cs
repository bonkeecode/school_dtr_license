using SchoolDTR.Models;
using SchoolDTR.Services;
using MySqlConnector;

namespace SchoolDTR.Forms;

public class SettingsForm : Form
{
    private readonly TextBox txtSchoolId = new();
    private readonly TextBox txtSchoolName = new();

    private readonly PictureBox picLogo = new();
    private readonly Button btnUploadLogo = new();
    private string selectedLogoPath = "";

    private readonly ComboBox cmbDeviceModel = new();
    private readonly TextBox txtDeviceIp = new();
    private readonly NumericUpDown numDevicePort = new();
    private readonly NumericUpDown numMachineNumber = new();
    private readonly TextBox txtSupervisorName = new();
    private readonly TextBox txtSupervisorPosition = new();

    private readonly TextBox txtDbHost = new();
    private readonly TextBox txtDbName = new();
    private readonly TextBox txtDbUser = new();
    private readonly TextBox txtDbPassword = new();
    

    public SettingsForm()
    {
        Text = "System Settings";
        Width = 650;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        LoadSettings();
        ApplyLogoIcon();
    }

    private void BuildUi()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 2,
            AutoScroll = true
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(panel, "School ID:", txtSchoolId);
        AddRow(panel, "School Name:", txtSchoolName);

        picLogo.Height = 90;
        picLogo.BorderStyle = BorderStyle.FixedSingle;
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        AddRow(panel, "School Logo:", picLogo, 100);

        btnUploadLogo.Text = "Upload Logo";
        btnUploadLogo.Height = 35;
        btnUploadLogo.Click += BtnUploadLogo_Click;
        AddRow(panel, "", btnUploadLogo);

        cmbDeviceModel.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbDeviceModel.Items.AddRange(new object[]
        {
            "ZKTeco Compatible",
            "ZKTeco K14",
            "ZKTeco K40",
            "ZKTeco MB460",
            "ZKTeco X628-C",
            "Other"
        });

        AddRow(panel, "Device Model:", cmbDeviceModel);
        AddRow(panel, "Device IP:", txtDeviceIp);

        numDevicePort.Minimum = 1;
        numDevicePort.Maximum = 99999;
        AddRow(panel, "Device Port:", numDevicePort);

        numMachineNumber.Minimum = 1;
        numMachineNumber.Maximum = 999;
        AddRow(panel, "Machine No.:", numMachineNumber);




        AddRow(panel, "DB Host:", txtDbHost);
        AddRow(panel, "DB Name:", txtDbName);
        AddRow(panel, "DB User:", txtDbUser);

        txtDbPassword.UseSystemPasswordChar = true;
        AddRow(panel, "DB Password:", txtDbPassword);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        var btnTestDb = new Button
        {
            Text = "Test DB",
            Width = 100,
            Height = 35
        };

        var btnTestDevice = new Button
        {
            Text = "Test Device",
            Width = 110,
            Height = 35
        };

        var btnSave = new Button
        {
            Text = "Save Settings",
            Width = 130,
            Height = 35
        };

        btnTestDb.Click += (_, _) => TestDatabaseConnection();
        btnTestDevice.Click += (_, _) => TestDeviceConnection();
        btnSave.Click += (_, _) => SaveSettings();

        buttons.Controls.Add(btnTestDb);
        buttons.Controls.Add(btnTestDevice);
        buttons.Controls.Add(btnSave);

        AddRow(panel, "", buttons, 50);

        Controls.Add(panel);
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control control, int height = 38)
    {
        int row = panel.RowCount;

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        panel.RowCount++;

        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        control.Dock = DockStyle.Fill;

        panel.Controls.Add(lbl, 0, row);
        panel.Controls.Add(control, 1, row);
    }

    private void LoadSettings()
    {
        var s = AppSettingsService.Load();

        txtSchoolId.Text = s.SchoolId;
        txtSchoolName.Text = s.SchoolName;
        selectedLogoPath = s.LogoPath ?? "";

        if (File.Exists(selectedLogoPath))
        {
            picLogo.Image?.Dispose();
            using var img = Image.FromFile(selectedLogoPath);
            picLogo.Image = new Bitmap(img);
        }

        cmbDeviceModel.Text = string.IsNullOrWhiteSpace(s.DeviceModel)
            ? "ZKTeco Compatible"
            : s.DeviceModel;

        txtDeviceIp.Text = s.DeviceIp;
        numDevicePort.Value = s.DevicePort > 0 ? s.DevicePort : 4370;
        numMachineNumber.Value = s.MachineNumber > 0 ? s.MachineNumber : 1;

        txtDbHost.Text = s.DbHost;
        txtDbName.Text = s.DbName;
        txtDbUser.Text = s.DbUser;
        txtDbPassword.Text = s.DbPassword;
    }

    private void SaveSettings()
    {
        var s = AppSettingsService.Load();

        s.SchoolId = txtSchoolId.Text.Trim();
        s.SchoolName = txtSchoolName.Text.Trim();
        s.LogoPath = selectedLogoPath;

        s.DeviceModel = cmbDeviceModel.Text;
        s.DeviceIp = txtDeviceIp.Text.Trim();
        s.DevicePort = (int)numDevicePort.Value;
        s.MachineNumber = (int)numMachineNumber.Value;

        s.DbHost = txtDbHost.Text.Trim();
        s.DbName = txtDbName.Text.Trim();
        s.DbUser = txtDbUser.Text.Trim();
        s.DbPassword = txtDbPassword.Text;

        AppSettingsService.Save(s);

        AuditLogService.Log("SETTINGS_UPDATED", "System settings were updated.");

        MessageBox.Show(
            "Settings saved successfully.\n\nRestart the system to fully apply the new logo and icon.",
            "Settings",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

        Close();
    }

    private void BtnUploadLogo_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select School Logo",
            Filter =
                "Image Files|*.png;*.jpg;*.jpeg;*.bmp"
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var settings = AppSettingsService.Load();

        string logoPath =
            AppSettingsService.GetDefaultLogoPath();

        using (var img = Image.FromFile(dlg.FileName))
        {
            img.Save(
                logoPath,
                System.Drawing.Imaging.ImageFormat.Png);
        }

        settings.LogoPath = logoPath;
        AppSettingsService.Save(settings);

        picLogo.Image?.Dispose();
        picLogo.Image = Image.FromFile(logoPath);

        ApplyLogoIcon();

        AppSettingsService.Save(settings);

        picLogo.Image?.Dispose();
        picLogo.Image = Image.FromFile(logoPath);

        MessageBox.Show(
            "Logo uploaded successfully.",
            "Success",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void TestDatabaseConnection()
    {
        try
        {
            var csb = new MySqlConnectionStringBuilder
            {
                Server = txtDbHost.Text.Trim(),
                Database = txtDbName.Text.Trim(),
                UserID = txtDbUser.Text.Trim(),
                Password = txtDbPassword.Text,
                SslMode = MySqlSslMode.None
            };

            using var conn = new MySqlConnection(csb.ConnectionString);
            conn.Open();

            MessageBox.Show(
                "Database connection successful.",
                "Test Database",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Database connection failed:\n\n" + ex.Message,
                "Test Database",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void TestDeviceConnection()
    {
        try
        {
            string ip = txtDeviceIp.Text.Trim();
            int port = (int)numDevicePort.Value;

            using var client = new System.Net.Sockets.TcpClient();

            var result = client.BeginConnect(ip, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

            if (!success || !client.Connected)
            {
                MessageBox.Show(
                    $"Cannot connect to device.\n\nIP: {ip}\nPort: {port}",
                    "Test Device",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            client.EndConnect(result);

            MessageBox.Show(
                $"Device connection successful.\n\nIP: {ip}\nPort: {port}",
                "Test Device",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Device connection failed:\n\n" + ex.Message,
                "Test Device",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
    private void ApplyLogoIcon()
    {
        var settings = AppSettingsService.Load();

        if (string.IsNullOrWhiteSpace(settings.LogoPath))
            return;

        if (!File.Exists(settings.LogoPath))
            return;

        try
        {
            using var bmp = new Bitmap(settings.LogoPath);
            IntPtr hIcon = bmp.GetHicon();
            Icon = Icon.FromHandle(hIcon);
        }
        catch
        {
            // Ignore invalid image/icon errors
        }
    }
}