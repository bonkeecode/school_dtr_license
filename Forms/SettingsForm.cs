using SchoolDTR.Services;
using MySqlConnector;
namespace SchoolDTR.Forms;

public class SettingsForm : Form
{
    private readonly TextBox txtSchoolId = new();
    private readonly TextBox txtSchoolName = new();

    private readonly ComboBox cmbDeviceModel = new();
    private readonly TextBox txtDeviceIp = new();
    private readonly NumericUpDown numDevicePort = new();
    private readonly NumericUpDown numMachineNumber = new();

    private readonly TextBox txtDbHost = new();
    private readonly TextBox txtDbName = new();
    private readonly TextBox txtDbUser = new();
    private readonly TextBox txtDbPassword = new();
  



    public SettingsForm()
    {
        Text = "System Settings";
        Width = 500;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        LoadSettings();
    }

        private void BuildUi()
        {
            Width = 600;
            Height = 520;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 2,
                RowCount = 0,
                AutoSize = false
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(panel, "School ID:", txtSchoolId);
            AddRow(panel, "School Name:", txtSchoolName);

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

            numDevicePort.Maximum = 99999;
            numDevicePort.Minimum = 1;
            AddRow(panel, "Device Port:", numDevicePort);

            numMachineNumber.Maximum = 999;
            numMachineNumber.Minimum = 1;
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

            int buttonRow = panel.RowCount;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            panel.RowCount++;
            panel.Controls.Add(new Label(), 0, buttonRow);
            panel.Controls.Add(buttons, 1, buttonRow);

            Controls.Add(panel);
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

        private static void AddRow(TableLayoutPanel panel, string label, Control control)
        {
            int row = panel.RowCount;

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
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
        cmbDeviceModel.Text = s.DeviceModel;
        numMachineNumber.Value = s.MachineNumber;
    }

    private void SaveSettings()
    {
        var s = new AppSettings
        {
            SchoolId = txtSchoolId.Text.Trim(),
            SchoolName = txtSchoolName.Text.Trim(),
            DeviceModel = cmbDeviceModel.Text,
            DeviceIp = txtDeviceIp.Text.Trim(),
            DevicePort = (int)numDevicePort.Value,
            MachineNumber = (int)numMachineNumber.Value,

            DbHost = txtDbHost.Text.Trim(),
            DbName = txtDbName.Text.Trim(),
            DbUser = txtDbUser.Text.Trim(),
            DbPassword = txtDbPassword.Text
        };

        AppSettingsService.Save(s);
        AuditLogService.Log("SETTINGS_UPDATED", "System settings were updated.");
        MessageBox.Show("Settings saved successfully.", "Settings");
        Close();
    }

    private void TestDeviceConnection()
    {
        try
        {
            string ip = txtDeviceIp.Text.Trim();

            if (!int.TryParse(numDevicePort.Value.ToString(), out int port))
            {
                MessageBox.Show(
                    "Invalid device port.",
                    "Test Device",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

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
}