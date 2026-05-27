using System;
using System.Data;
using System.Windows.Forms;
using MySqlConnector;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class RawLogsForm : Form
{
    private readonly DateTimePicker dtFrom = new();
    private readonly DateTimePicker dtTo = new();
    private readonly TextBox txtSearch = new();
    private readonly DataGridView grid = new();

    public RawLogsForm()
    {
        Text = "Raw Biometric Logs";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        EnsureTable();
        LoadLogs();
    }

    private void BuildUi()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(15)
        };

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        dtFrom.Width = 120;
        dtFrom.Format = DateTimePickerFormat.Short;
        dtFrom.Value = DateTime.Today.AddDays(-7);

        dtTo.Width = 120;
        dtTo.Format = DateTimePickerFormat.Short;
        dtTo.Value = DateTime.Today;

        txtSearch.Width = 250;
        txtSearch.PlaceholderText = "Biometric User ID";

        var btnLoad = new Button
        {
            Text = "Load",
            Width = 100,
            Height = 32
        };
        btnLoad.Click += (_, _) => LoadLogs();

        top.Controls.Add(new Label { Text = "From:", AutoSize = true, Padding = new Padding(0, 8, 5, 0) });
        top.Controls.Add(dtFrom);

        top.Controls.Add(new Label { Text = "To:", AutoSize = true, Padding = new Padding(15, 8, 5, 0) });
        top.Controls.Add(dtTo);

        top.Controls.Add(new Label { Text = "Search:", AutoSize = true, Padding = new Padding(15, 8, 5, 0) });
        top.Controls.Add(txtSearch);
        top.Controls.Add(btnLoad);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        main.Controls.Add(top, 0, 0);
        main.Controls.Add(grid, 0, 1);

        Controls.Add(main);
    }

    private void EnsureTable()
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            CREATE TABLE IF NOT EXISTS biometric_raw_logs (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                school_id VARCHAR(20) NOT NULL,
                biometric_user_id VARCHAR(50) NOT NULL,
                punch_time DATETIME NOT NULL,
                punch_type VARCHAR(30) NULL,
                device_serial VARCHAR(100) NULL,
                fetched_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                KEY idx_school_id (school_id),
                KEY idx_biometric_user_id (biometric_user_id),
                KEY idx_punch_time (punch_time),
                UNIQUE KEY uq_raw_log (school_id, biometric_user_id, punch_time)
            );
        ", conn);

        cmd.ExecuteNonQuery();
    }

    private void LoadLogs()
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT
                id,
                school_id,
                biometric_user_id,
                punch_time,
                punch_type,
                device_serial,
                fetched_at
            FROM biometric_raw_logs
            WHERE DATE(punch_time) BETWEEN @from AND @to
              AND (
                    @search = ''
                    OR biometric_user_id LIKE CONCAT('%', @search, '%')
                    OR school_id LIKE CONCAT('%', @search, '%')
                    OR punch_type LIKE CONCAT('%', @search, '%')
                    OR device_serial LIKE CONCAT('%', @search, '%')
                  )
            ORDER BY punch_time DESC;
        ", conn);

        cmd.Parameters.AddWithValue("@from", dtFrom.Value.Date);
        cmd.Parameters.AddWithValue("@to", dtTo.Value.Date);
        cmd.Parameters.AddWithValue("@search", txtSearch.Text.Trim());

        using var da = new MySqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);

        grid.DataSource = dt;
    }
}