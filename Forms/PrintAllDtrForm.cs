using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using MySqlConnector;
using SchoolDTR.Models;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class PrintAllDtrForm : Form
{
    private readonly DateTimePicker dtMonth = new();
    private readonly PrintDocument printDoc = new();

    private readonly List<string> employeeNos = new();
    private int currentEmployeeIndex;
    private EmployeeDtrPrintData? currentData;

    public PrintAllDtrForm()
    {
        Text = "AO Print All DTR - CSC Form 48";
        Width = 520;
        Height = 240;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();

        printDoc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        printDoc.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
        printDoc.DefaultPageSettings.Landscape = false;

        printDoc.BeginPrint += PrintDoc_BeginPrint;
        printDoc.PrintPage += PrintDoc_PrintPage;
    }

    private void BuildUi()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ColumnCount = 2,
            RowCount = 5
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label { Text = "Month", Dock = DockStyle.Fill }, 0, 0);

        dtMonth.Format = DateTimePickerFormat.Custom;
        dtMonth.CustomFormat = "MMMM yyyy";
        dtMonth.ShowUpDown = true;
        dtMonth.Dock = DockStyle.Fill;
        panel.Controls.Add(dtMonth, 1, 0);

        var btnPreview = new Button
        {
            Text = "Print Preview All",
            Dock = DockStyle.Fill,
            Height = 40
        };
        btnPreview.Click += (_, _) => PreviewAll();
        panel.Controls.Add(btnPreview, 1, 1);

        var btnPrint = new Button
        {
            Text = "Print All",
            Dock = DockStyle.Fill,
            Height = 40
        };
        btnPrint.Click += (_, _) => PrintAll();
        panel.Controls.Add(btnPrint, 1, 2);

        var btnPdf = new Button
        {
            Text = "Export All DTR to PDF",
            Dock = DockStyle.Fill,
            Height = 40
        };
        btnPdf.Click += (_, _) => ExportAllToPdf();
        panel.Controls.Add(btnPdf, 1, 3);

        Controls.Add(panel);
    }

    private bool PrepareEmployees()
    {
        LoadEmployeeNos();

        if (employeeNos.Count == 0)
        {
            MessageBox.Show("No DTR records found for this month.");
            return false;
        }

        ResetPrintState();
        return true;
    }

    private void ResetPrintState()
    {
        currentEmployeeIndex = 0;
        currentData = employeeNos.Count > 0
            ? LoadPrintData(employeeNos[currentEmployeeIndex])
            : null;
    }

    private void PreviewAll()
    {
        if (!PrepareEmployees())
            return;

        using var preview = new PrintPreviewDialog
        {
            Document = printDoc,
            Width = 1000,
            Height = 700
        };

        preview.ShowDialog(this);
    }

    private void PrintAll()
    {
        if (!PrepareEmployees())
            return;

        using var dlg = new PrintDialog
        {
            Document = printDoc,
            AllowSomePages = false,
            UseEXDialog = true
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            printDoc.PrinterSettings = dlg.PrinterSettings;
            printDoc.Print();
        }
    }

    private void ExportAllToPdf()
    {
        if (!PrepareEmployees())
            return;

        using var save = new SaveFileDialog
        {
            Title = "Export All DTR to PDF",
            Filter = "PDF file (*.pdf)|*.pdf",
            FileName = $"DTR_All_{dtMonth.Value:yyyy_MM}.pdf",
            OverwritePrompt = true
        };

        if (save.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var oldPrinterSettings = printDoc.PrinterSettings;
            var oldController = printDoc.PrintController;

            printDoc.PrinterSettings = new PrinterSettings
            {
                PrinterName = "Microsoft Print to PDF",
                PrintToFile = true,
                PrintFileName = save.FileName
            };

            printDoc.PrintController = new StandardPrintController();

            if (!printDoc.PrinterSettings.IsValid)
                throw new InvalidOperationException("Microsoft Print to PDF printer is not available on this computer.");

            printDoc.Print();

            printDoc.PrinterSettings = oldPrinterSettings;
            printDoc.PrintController = oldController;

            MessageBox.Show("DTR PDF export completed.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("PDF export failed: " + ex.Message);
        }
    }

    private void PrintDoc_BeginPrint(object? sender, PrintEventArgs e)
    {
        ResetPrintState();
    }

    private void LoadEmployeeNos()
    {
        employeeNos.Clear();

        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT DISTINCT
                COALESCE(NULLIF(employee_no, ''), NULLIF(employee_id, '')) AS employee_no
            FROM biometric_dtr
            WHERE YEAR(log_date) = @year
            AND MONTH(log_date) = @month
            AND COALESCE(NULLIF(employee_no, ''), NULLIF(employee_id, '')) IS NOT NULL
            ORDER BY employee_no ASC;
        ", conn);

        cmd.Parameters.AddWithValue("@year", dtMonth.Value.Year);
        cmd.Parameters.AddWithValue("@month", dtMonth.Value.Month);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var employeeNo = Convert.ToString(reader["employee_no"])?.Trim();

            if (!string.IsNullOrWhiteSpace(employeeNo))
                employeeNos.Add(employeeNo);
        }
    }

    private EmployeeDtrPrintData? LoadPrintData(string employeeNo)
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT
                COALESCE(NULLIF(d.employee_no, ''), NULLIF(d.employee_id, '')) AS employee_no,
                d.biometric_user_id,
                d.employee_name,
                d.log_date,
                d.morning_in,
                d.morning_out,
                d.afternoon_in,
                d.afternoon_out,
                d.remarks,
                e.position_title,
                e.school_id,
                e.immediate_supervisor_name,
                e.immediate_supervisor_position
            FROM biometric_dtr d
            LEFT JOIN employees e
                ON e.employee_no = COALESCE(NULLIF(d.employee_no, ''), NULLIF(d.employee_id, ''))
            WHERE COALESCE(NULLIF(d.employee_no, ''), NULLIF(d.employee_id, '')) = @employee_no
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
            ImmediateSupervisorName = Convert.ToString(first["immediate_supervisor_name"]) ?? "",
            ImmediateSupervisorPosition = Convert.ToString(first["immediate_supervisor_position"]) ?? "",
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
        while (currentData == null && currentEmployeeIndex < employeeNos.Count - 1)
        {
            currentEmployeeIndex++;
            currentData = LoadPrintData(employeeNos[currentEmployeeIndex]);
        }

        if (e.Graphics == null || currentData == null)
        {
            e.HasMorePages = false;
            return;
        }

        CscForm48Printer.DrawForm(e.Graphics, e.PageBounds, currentData);

        currentEmployeeIndex++;

        if (currentEmployeeIndex < employeeNos.Count)
        {
            currentData = LoadPrintData(employeeNos[currentEmployeeIndex]);
            e.HasMorePages = true;
        }
        else
        {
            currentData = null;
            e.HasMorePages = false;
        }
    }
}