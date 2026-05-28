using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;
using SchoolDTR.Services;
using System.Drawing.Printing;
using SchoolDTR.Models;

namespace SchoolDTR.Forms;

public class DtrViewerForm : Form
{
    private readonly DateTimePicker dtMonth = new();
    private readonly TextBox txtSearch = new();
    private readonly DataGridView gridEmployees = new();
    private readonly DataGridView gridDtr = new();

    public DtrViewerForm()
    {
        Text = "View DTR";
        Width = 1200;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        LoadEmployeesWithDtr();
    }

    private void BuildUi()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(15)
        };

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        dtMonth.Format = DateTimePickerFormat.Custom;
        dtMonth.CustomFormat = "MMMM yyyy";
        dtMonth.ShowUpDown = true;
        dtMonth.Width = 160;

        txtSearch.Width = 300;
        txtSearch.PlaceholderText = "Search employee no, biometric id, or name...";
        txtSearch.TextChanged += (_, _) => LoadEmployeesWithDtr();

        var btnLoad = new Button
        {
            Text = "Load",
            Width = 100,
            Height = 35
        };
        btnLoad.Click += (_, _) => LoadEmployeesWithDtr();

        var btnPrint = new Button
        {
            Text = "Print Selected DTR",
            Width = 170,
            Height = 35
        };
        btnPrint.Click += (_, _) => PrintSelectedEmployee();

        top.Controls.Add(new Label
        {
            Text = "Month:",
            AutoSize = true,
            Padding = new Padding(0, 8, 5, 0)
        });
        top.Controls.Add(dtMonth);

        top.Controls.Add(new Label
        {
            Text = "Search:",
            AutoSize = true,
            Padding = new Padding(20, 8, 5, 0)
        });
        top.Controls.Add(txtSearch);
        top.Controls.Add(btnLoad);
        top.Controls.Add(btnPrint);
        gridEmployees.Dock = DockStyle.Fill;
        gridEmployees.ReadOnly = true;
        gridEmployees.AllowUserToAddRows = false;
        gridEmployees.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridEmployees.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridEmployees.CellClick += (_, _) => LoadSelectedEmployeeDtr();
        gridEmployees.CellDoubleClick += (_, _) => LoadSelectedEmployeeDtr();

        gridDtr.Dock = DockStyle.Fill;
        gridDtr.ReadOnly = true;
        gridDtr.AllowUserToAddRows = false;
        gridDtr.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridDtr.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        main.Controls.Add(top, 0, 0);
        main.Controls.Add(gridEmployees, 0, 1);
        main.Controls.Add(gridDtr, 0, 2);

        Controls.Add(main);
    }

    private void LoadEmployeesWithDtr()
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT
                COALESCE(NULLIF(d.employee_no, ''), NULLIF(d.employee_id, '')) AS employee_no,
                d.biometric_user_id,
                d.employee_name,
                COUNT(*) AS dtr_days,
                SUM(
                    CASE
                        WHEN IFNULL(d.morning_in, '') = ''
                        AND IFNULL(d.morning_out, '') = ''
                        AND IFNULL(d.afternoon_in, '') = ''
                        AND IFNULL(d.afternoon_out, '') = ''
                        THEN 1 ELSE 0
                    END
                ) AS blank_days
            FROM biometric_dtr d
            WHERE YEAR(d.log_date) = @year
            AND MONTH(d.log_date) = @month
            AND (
                    @search = ''
                    OR COALESCE(NULLIF(d.employee_no, ''), NULLIF(d.employee_id, '')) LIKE CONCAT('%', @search, '%')
                    OR d.biometric_user_id LIKE CONCAT('%', @search, '%')
                    OR d.employee_name LIKE CONCAT('%', @search, '%')
                )
            GROUP BY
                COALESCE(NULLIF(d.employee_no, ''), NULLIF(d.employee_id, '')),
                d.biometric_user_id,
                d.employee_name
            ORDER BY d.employee_name ASC;
        ", conn);

        cmd.Parameters.AddWithValue("@year", dtMonth.Value.Year);
        cmd.Parameters.AddWithValue("@month", dtMonth.Value.Month);
        cmd.Parameters.AddWithValue("@search", txtSearch.Text.Trim());

        using var da = new MySqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);

        gridEmployees.DataSource = dt;

        gridDtr.DataSource = null;

        if (dt.Rows.Count > 0)
        {
            gridEmployees.ClearSelection();
            gridEmployees.Rows[0].Selected = true;
            LoadSelectedEmployeeDtr();
        }
    }

    private void LoadSelectedEmployeeDtr()
    {
        if (gridEmployees.CurrentRow == null)
            return;

        string employeeNo =
            Convert.ToString(gridEmployees.CurrentRow.Cells["employee_no"].Value) ?? "";

        if (employeeNo == "")
            return;

        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT
                log_date,
                morning_in,
                morning_out,
                afternoon_in,
                afternoon_out,
                remarks
            FROM biometric_dtr
            WHERE COALESCE(NULLIF(employee_no, ''), NULLIF(employee_id, '')) = @employee_no
            AND YEAR(log_date) = @year
            AND MONTH(log_date) = @month
            ORDER BY log_date ASC;
        ", conn);

        cmd.Parameters.AddWithValue("@employee_no", employeeNo);
        cmd.Parameters.AddWithValue("@year", dtMonth.Value.Year);
        cmd.Parameters.AddWithValue("@month", dtMonth.Value.Month);

        using var da = new MySqlDataAdapter(cmd);
        var raw = new DataTable();
        da.Fill(raw);

        gridDtr.DataSource = BuildDisplayTable(raw);
    }

        private void PrintSelectedEmployee()
        {
            if (gridEmployees.CurrentRow == null)
            {
                MessageBox.Show(
                    "Please select an employee first.",
                    "No Employee Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                string biometricUserId =
                    Convert.ToString(
                        gridEmployees.CurrentRow.Cells["biometric_user_id"].Value
                    ) ?? "";

                if (string.IsNullOrWhiteSpace(biometricUserId))
                {
                    MessageBox.Show("Employee number not found.");
                    return;
                }

                using var conn = Db.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        employee_no,
                        employee_name,
                        employee_id,
                        '' AS school_id,
                        log_date AS work_date,
                        morning_in,
                        morning_out,
                        afternoon_in,
                        afternoon_out,
                        remarks
                    FROM biometric_dtr
                    WHERE biometric_user_id = @biometricUserId
                        AND YEAR(log_date) = @year
                        AND MONTH(log_date) = @month
                    ORDER BY log_date;
                ";

                cmd.Parameters.AddWithValue(
                        "@biometricUserId",
                        biometricUserId
                    );
                cmd.Parameters.AddWithValue("@year", dtMonth.Value.Year);
                cmd.Parameters.AddWithValue("@month", dtMonth.Value.Month);

                var table = new DataTable();

                using (var da = new MySqlDataAdapter(cmd))
                {
                    da.Fill(table);
                }

                if (table.Rows.Count == 0)
                {
                    MessageBox.Show("No DTR found for selected employee.");
                    return;
                }

                var first = table.Rows[0];

                var data = new EmployeeDtrPrintData
                {
                    EmployeeNo = Convert.ToString(first["employee_no"]) ?? "",
                    EmployeeName = Convert.ToString(first["employee_name"]) ?? "",
                    PositionTitle = "",
                    SchoolId = Convert.ToString(first["school_id"]) ?? "",
                    Month = new DateTime(
                        dtMonth.Value.Year,
                        dtMonth.Value.Month,
                        1
                    )
                };

                foreach (DataRow row in table.Rows)
                {
                    data.Rows.Add(new EmployeeDtrPrintRow
                    {
                        Date = Convert.ToDateTime(row["work_date"]),
                        MorningIn = Convert.ToString(row["morning_in"]) ?? "",
                        MorningOut = Convert.ToString(row["morning_out"]) ?? "",
                        AfternoonIn = Convert.ToString(row["afternoon_in"]) ?? "",
                        AfternoonOut = Convert.ToString(row["afternoon_out"]) ?? "",
                        Remarks = Convert.ToString(row["remarks"]) ?? ""
                    });
                }

                var printDoc = new PrintDocument
                {
                    DefaultPageSettings =
                    {
                        Landscape = false,
                        Margins = new Margins(20, 20, 20, 20),
                        PaperSize = new PaperSize("A4", 827, 1169)
                    }
                };

                printDoc.PrintPage += (_, e) =>
                {
                    var bounds = new Rectangle(
                        0,
                        0,
                        e.PageBounds.Width,
                        e.PageBounds.Height
                    );

                    CscForm48Printer.DrawForm(
                        e.Graphics,
                        bounds,
                        data
                    );
                };

                using var preview = new PrintPreviewDialog
                {
                    Document = printDoc,
                    Width = 1200,
                    Height = 900
                };

                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to print DTR.\n\n" + ex.Message,
                    "Print Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

    private DataTable BuildDisplayTable(DataTable raw)
    {
        var dt = new DataTable();

        dt.Columns.Add("Date");
        dt.Columns.Add("AM In");
        dt.Columns.Add("AM Out");
        dt.Columns.Add("PM In");
        dt.Columns.Add("PM Out");
        dt.Columns.Add("Merged Status");
        dt.Columns.Add("Remarks");

        foreach (DataRow row in raw.Rows)
        {
            var morningIn = Convert.ToString(row["morning_in"]) ?? "";
            var morningOut = Convert.ToString(row["morning_out"]) ?? "";
            var afternoonIn = Convert.ToString(row["afternoon_in"]) ?? "";
            var afternoonOut = Convert.ToString(row["afternoon_out"]) ?? "";

            var merged = GetMergedStatus(
                morningIn,
                morningOut,
                afternoonIn,
                afternoonOut
            );

            dt.Rows.Add(
                Convert.ToDateTime(row["log_date"]).ToString("yyyy-MM-dd"),
                merged == "" ? morningIn : "",
                merged == "" ? morningOut : "",
                merged == "" ? afternoonIn : "",
                merged == "" ? afternoonOut : "",
                merged,
                ""
            );
        }

        return dt;
    }

    private string GetMergedStatus(
        string morningIn,
        string morningOut,
        string afternoonIn,
        string afternoonOut)
    {
        morningIn = morningIn.Trim();
        morningOut = morningOut.Trim();
        afternoonIn = afternoonIn.Trim();
        afternoonOut = afternoonOut.Trim();

        if (morningIn == "" ||
            morningOut == "" ||
            afternoonIn == "" ||
            afternoonOut == "")
            return "";

        if (morningIn == morningOut &&
            morningOut == afternoonIn &&
            afternoonIn == afternoonOut)
            return morningIn;

        return "";
    }
    
}