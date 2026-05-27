using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
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

    private DataTable currentLogs = new();
    private int printIndex = 0;

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
        dtFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        dtTo.Width = 120;
        dtTo.Format = DateTimePickerFormat.Short;
        dtTo.Value = DateTime.Today;

        txtSearch.Width = 250;
        txtSearch.PlaceholderText = "Employee name";

        var btnLoad = new Button
        {
            Text = "Load",
            Width = 100,
            Height = 32
        };
        btnLoad.Click += (_, _) => LoadLogs();

        var btnPrint = new Button
        {
            Text = "Print",
            Width = 100,
            Height = 32
        };
        btnPrint.Click += (_, _) => PrintLogs();

        top.Controls.Add(new Label { Text = "From:", AutoSize = true, Padding = new Padding(0, 8, 5, 0) });
        top.Controls.Add(dtFrom);

        top.Controls.Add(new Label { Text = "To:", AutoSize = true, Padding = new Padding(15, 8, 5, 0) });
        top.Controls.Add(dtTo);

        top.Controls.Add(new Label { Text = "Search Name:", AutoSize = true, Padding = new Padding(15, 8, 5, 0) });
        top.Controls.Add(txtSearch);
        top.Controls.Add(btnLoad);
        top.Controls.Add(btnPrint);

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
                e.full_name AS `Name`,
                r.biometric_user_id AS `Biometric ID`,
                r.punch_time AS `Punch Time`,
                CASE
                    WHEN r.punch_type = '0' THEN 'TIME IN'
                    WHEN r.punch_type = '1' THEN 'TIME OUT'
                    ELSE r.punch_type
                END AS `Punch Type`,
                r.fetched_at AS `Fetched At`
            FROM biometric_raw_logs r
            LEFT JOIN employees e
                ON e.biometric_user_id = r.biometric_user_id
               AND e.school_id = r.school_id
            WHERE DATE(r.punch_time) BETWEEN @from AND @to
              AND (
                    @search = ''
                    OR e.full_name LIKE CONCAT('%', @search, '%')
                  )
            ORDER BY r.punch_time ASC;
        ", conn);

        cmd.Parameters.AddWithValue("@from", dtFrom.Value.Date);
        cmd.Parameters.AddWithValue("@to", dtTo.Value.Date);
        cmd.Parameters.AddWithValue("@search", txtSearch.Text.Trim());

        using var da = new MySqlDataAdapter(cmd);
        currentLogs = new DataTable();
        da.Fill(currentLogs);

        grid.DataSource = currentLogs;
    }

    private void PrintLogs()
    {
        if (currentLogs.Rows.Count == 0)
        {
            MessageBox.Show("No logs to print.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var employeeCount = currentLogs.AsEnumerable()
            .Select(r => r["Biometric ID"]?.ToString())
            .Distinct()
            .Count();

        if (employeeCount > 1)
        {
            MessageBox.Show(
                "Please search/load one employee only before printing.",
                "Print Raw Logs",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        printIndex = 0;

        var doc = new PrintDocument();
        doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        doc.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
        doc.PrintPage += PrintPage;

        using var preview = new PrintPreviewDialog
        {
            Document = doc,
            Width = 1000,
            Height = 700
        };

        preview.ShowDialog(this);
    }

    private void PrintPage(object? sender, PrintPageEventArgs e)
    {
        var g = e.Graphics;
        var bounds = e.MarginBounds;

        using var titleFont = new Font("Arial", 14, FontStyle.Bold);
        using var headerFont = new Font("Arial", 10, FontStyle.Bold);
        using var textFont = new Font("Arial", 9);
        using var smallFont = new Font("Arial", 8);

        int y = bounds.Top;

        string name = currentLogs.Rows[0]["Name"]?.ToString() ?? "";
        string bioId = currentLogs.Rows[0]["Biometric ID"]?.ToString() ?? "";

        g.DrawString("RAW BIOMETRIC LOGS", titleFont, Brushes.Black, bounds.Left, y);
        y += 30;

        g.DrawString($"Name: {name}", headerFont, Brushes.Black, bounds.Left, y);
        y += 20;

        g.DrawString($"Biometric ID: {bioId}", textFont, Brushes.Black, bounds.Left, y);
        y += 18;

        g.DrawString($"Period: {dtFrom.Value:MMMM dd, yyyy} to {dtTo.Value:MMMM dd, yyyy}", textFont, Brushes.Black, bounds.Left, y);
        y += 18;

        g.DrawString($"Print Date: {DateTime.Now:MMMM dd, yyyy hh:mm tt}", textFont, Brushes.Black, bounds.Left, y);
        y += 30;

        g.DrawLine(Pens.Black, bounds.Left, y, bounds.Right, y);
        y += 15;

        int columnCount = 3;
        int columnWidth = bounds.Width / columnCount;
        int rowHeight = 18;

        int startY = y;
        int maxY = bounds.Bottom - 70;

        while (printIndex < currentLogs.Rows.Count)
        {
            int relativeIndex = printIndex;
            int col = relativeIndex % columnCount;

            int x = bounds.Left + (col * columnWidth);
            int currentY = startY + ((relativeIndex / columnCount) * rowHeight);

            if (currentY > maxY)
            {
                e.HasMorePages = true;
                return;
            }

            var row = currentLogs.Rows[printIndex];

            DateTime punchTime = Convert.ToDateTime(row["Punch Time"]);
            string punchType = row["Punch Type"]?.ToString() ?? "";

            string line = $"{punchTime:MM/dd/yyyy hh:mm tt} - {punchType}";

            g.DrawString(line, smallFont, Brushes.Black, x, currentY);

            printIndex++;
        }

        int footerY = bounds.Bottom - 50;

        int timeInCount = currentLogs.AsEnumerable()
            .Count(r => (r["Punch Type"]?.ToString() ?? "") == "TIME IN");

        int timeOutCount = currentLogs.AsEnumerable()
            .Count(r => (r["Punch Type"]?.ToString() ?? "") == "TIME OUT");

        g.DrawLine(Pens.Black, bounds.Left, footerY, bounds.Right, footerY);
        footerY += 12;

        g.DrawString($"Total TIME IN: {timeInCount}", headerFont, Brushes.Black, bounds.Left, footerY);
        g.DrawString($"Total TIME OUT: {timeOutCount}", headerFont, Brushes.Black, bounds.Left + 200, footerY);

        e.HasMorePages = false;
    }
}