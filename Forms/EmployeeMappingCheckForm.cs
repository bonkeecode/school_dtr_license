using System.Data;
using MySqlConnector;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class EmployeeMappingCheckForm : Form
{
    private readonly TabControl tabs = new();

    public EmployeeMappingCheckForm()
    {
        Text = "Employee Mapping Check";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        tabs.Dock = DockStyle.Fill;
        Controls.Add(tabs);

        LoadChecks();
    }

    private void LoadChecks()
    {
        tabs.TabPages.Clear();

        AddTab("Missing Bio ID", @"
            SELECT
                full_name AS `Employee Name`,
                school_id AS `School ID`,
                biometric_user_id AS `Biometric ID`
            FROM employees
            WHERE biometric_user_id IS NULL
            OR TRIM(biometric_user_id) = ''
            ORDER BY full_name;
        ");

        AddTab("Unmapped Device IDs", @"
            SELECT DISTINCT
                r.biometric_user_id AS `Biometric ID`,
                COUNT(*) AS `Log Count`,
                MIN(r.punch_time) AS `First Log`,
                MAX(r.punch_time) AS `Last Log`
            FROM biometric_raw_logs r
            LEFT JOIN employees e
                ON e.biometric_user_id = r.biometric_user_id
               AND e.school_id = r.school_id
            WHERE e.id IS NULL
            GROUP BY r.biometric_user_id
            ORDER BY r.biometric_user_id;
        ");

        AddTab("Duplicate Bio ID", @"
            SELECT
                biometric_user_id AS `Biometric ID`,
                COUNT(*) AS `Employee Count`
            FROM employees
            WHERE biometric_user_id IS NOT NULL
              AND TRIM(biometric_user_id) <> ''
            GROUP BY biometric_user_id
            HAVING COUNT(*) > 1
            ORDER BY biometric_user_id;
        ");
    }

    private void AddTab(string title, string sql)
    {
        var page = new TabPage(title);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(sql, conn);
        using var da = new MySqlDataAdapter(cmd);

        var dt = new DataTable();
        da.Fill(dt);

        grid.DataSource = dt;

        page.Controls.Add(grid);
        tabs.TabPages.Add(page);
    }
}