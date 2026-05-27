using System;
using System.Data;
using System.Drawing.Printing;
using System.Windows.Forms;
using MySqlConnector;
using SchoolDTR.Models;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class PrintDtrForm : Form
{
    private readonly TextBox txtEmployeeNo = new();
    private readonly DateTimePicker dtMonth = new();
    private readonly PrintDocument printDoc = new();

    private EmployeeDtrPrintData? printData;

    public PrintDtrForm()
    {
        Text = "Print DTR - CSC Form 48";
        Width = 500;
        Height = 220;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();

        printDoc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        printDoc.DefaultPageSettings.Landscape = false;
        printDoc.PrintPage += PrintDoc_PrintPage;
    }

    private void BuildUi()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ColumnCount = 2,
            RowCount = 4
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label { Text = "Employee No", Dock = DockStyle.Fill }, 0, 0);
        txtEmployeeNo.Dock = DockStyle.Fill;
        panel.Controls.Add(txtEmployeeNo, 1, 0);

        panel.Controls.Add(new Label { Text = "Month", Dock = DockStyle.Fill }, 0, 1);
        dtMonth.Format = DateTimePickerFormat.Custom;
        dtMonth.CustomFormat = "MMMM yyyy";
        dtMonth.ShowUpDown = true;
        dtMonth.Dock = DockStyle.Fill;
        panel.Controls.Add(dtMonth, 1, 1);

        var btnPreview = new Button
        {
            Text = "Print Preview",
            Dock = DockStyle.Fill,
            Height = 38
        };
        btnPreview.Click += (_, _) => Preview();

        panel.Controls.Add(btnPreview, 1, 2);

        Controls.Add(panel);
    }

    private void Preview()
    {
        if (txtEmployeeNo.Text.Trim() == "")
        {
            MessageBox.Show("Please enter Employee No.");
            return;
        }

        printData = LoadPrintData(txtEmployeeNo.Text.Trim());

        if (printData == null || printData.Rows.Count == 0)
        {
            MessageBox.Show("No DTR record found.");
            return;
        }

        using var preview = new PrintPreviewDialog
        {
            Document = printDoc,
            Width = 1000,
            Height = 750
        };

        preview.ShowDialog();
    }

    private EmployeeDtrPrintData? LoadPrintData(string employeeNo)
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT
                d.employee_no,
                d.biometric_user_id,
                d.employee_name,
                d.log_date,
                d.morning_in,
                d.morning_out,
                d.afternoon_in,
                d.afternoon_out,
                d.remarks,
                e.position_title,
                e.school_id
            FROM biometric_dtr d
            LEFT JOIN employees e
                ON e.employee_no = d.employee_no
            WHERE d.employee_no = @employee_no
              AND YEAR(d.log_date) = @year
              AND MONTH(d.log_date) = @month
            ORDER BY d.log_date ASC;
        ", conn);

        cmd.Parameters.AddWithValue("@employee_no", employeeNo);
        cmd.Parameters.AddWithValue("@year", dtMonth.Value.Year);
        cmd.Parameters.AddWithValue("@month", dtMonth.Value.Month);

        using var da = new MySqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);

        if (dt.Rows.Count == 0)
            return null;

        var first = dt.Rows[0];

        var data = new EmployeeDtrPrintData
        {
            EmployeeNo = Convert.ToString(first["employee_no"]) ?? "",
            EmployeeName = Convert.ToString(first["employee_name"]) ?? "",
            PositionTitle = Convert.ToString(first["position_title"]) ?? "",
            SchoolId = Convert.ToString(first["school_id"]) ?? "",
            Month = new DateTime(dtMonth.Value.Year, dtMonth.Value.Month, 1)
        };

        foreach (DataRow row in dt.Rows)
        {
            data.Rows.Add(new EmployeeDtrPrintRow
            {
                Date = Convert.ToDateTime(row["log_date"]),
                MorningIn = Convert.ToString(row["morning_in"]) ?? "",
                MorningOut = Convert.ToString(row["morning_out"]) ?? "",
                AfternoonIn = Convert.ToString(row["afternoon_in"]) ?? "",
                AfternoonOut = Convert.ToString(row["afternoon_out"]) ?? "",
                Remarks = Convert.ToString(row["remarks"]) ?? ""
            });
        }

        return data;
    }

    private void PrintDoc_PrintPage(object? sender, PrintPageEventArgs e)
    {
        if (e.Graphics == null || printData == null)
            return;

        CscForm48Printer.DrawForm(e.Graphics, e.MarginBounds, printData);
        e.HasMorePages = false;
    }
}