using System.Net.Sockets;
using MySqlConnector;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class HealthCheckForm : Form
{
    private readonly ListBox list = new();
    private readonly Button btnRun = new();

    public HealthCheckForm()
    {
        Text = "System Health Check";
        Width = 650;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        RunChecks();
    }

    private void BuildUi()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(15)
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        btnRun.Text = "Run Health Check";
        btnRun.Width = 160;
        btnRun.Height = 35;
        btnRun.Click += (_, _) => RunChecks();

        list.Dock = DockStyle.Fill;
        list.Font = new Font("Consolas", 10);

        panel.Controls.Add(btnRun, 0, 0);
        panel.Controls.Add(list, 0, 1);

        Controls.Add(panel);
    }

    private void RunChecks()
    {
        list.Items.Clear();

        CheckSettings();
        CheckDatabase();
        CheckTables();
        CheckDevice();
    }

    private void AddOk(string message)
    {
        list.Items.Add("✔ " + message);
    }

    private void AddFail(string message)
    {
        list.Items.Add("✘ " + message);
    }

    private void CheckSettings()
    {
        var s = AppSettingsService.Load();

        if (string.IsNullOrWhiteSpace(s.SchoolId))
            AddFail("School ID is empty.");
        else
            AddOk("School ID configured: " + s.SchoolId);

        if (string.IsNullOrWhiteSpace(s.SchoolName))
            AddFail("School Name is empty.");
        else
            AddOk("School Name configured: " + s.SchoolName);

        if (string.IsNullOrWhiteSpace(s.DeviceIp))
            AddFail("Device IP is empty.");
        else
            AddOk("Device IP configured: " + s.DeviceIp);
    }

    private void CheckDatabase()
    {
        try
        {
            using var conn = Db.GetConnection();
            conn.Open();

            AddOk("Database connection successful.");
        }
        catch (Exception ex)
        {
            AddFail("Database connection failed: " + ex.Message);
        }
    }

    private void CheckTables()
    {
        string[] requiredTables =
        {
            "employees",
            "biometric_raw_logs",
            "daily_dtr",
            "dtr_events",
            "holidays",
            "school_settings",
            "schools",
            "biometric_devices",
            "biometric_fetch_logs"
        };

        try
        {
            using var conn = Db.GetConnection();
            conn.Open();

            foreach (var table in requiredTables)
            {
                using var cmd = new MySqlCommand(@"
                    SELECT COUNT(*)
                    FROM information_schema.tables
                    WHERE table_schema = DATABASE()
                      AND table_name = @table;
                ", conn);

                cmd.Parameters.AddWithValue("@table", table);

                var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;

                if (exists)
                    AddOk("Table exists: " + table);
                else
                    AddFail("Missing table: " + table);
            }
        }
        catch (Exception ex)
        {
            AddFail("Table check failed: " + ex.Message);
        }
    }

    private void CheckDevice()
    {
        var s = AppSettingsService.Load();

        try
        {
            using var client = new TcpClient();

            var result = client.BeginConnect(s.DeviceIp, s.DevicePort, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

            if (!success || !client.Connected)
            {
                AddFail($"Device not reachable: {s.DeviceIp}:{s.DevicePort}");
                return;
            }

            client.EndConnect(result);
            AddOk($"Device reachable: {s.DeviceIp}:{s.DevicePort}");
        }
        catch (Exception ex)
        {
            AddFail("Device check failed: " + ex.Message);
        }
    }
}