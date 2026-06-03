using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class EmployeeForm : Form
{
    private readonly TextBox txtEmployeeId = new();
    private readonly TextBox txtFullName = new();
    private readonly TextBox txtDepartment = new();
    private readonly TextBox txtPosition = new();
    private readonly TextBox txtImmediateSupervisor = new();
    private readonly TextBox txtImmediateSupervisorPosition = new();
    private readonly TextBox txtSearch = new();
    private readonly DataGridView grid = new();

    public EmployeeForm()
    {
        Text = "Manage Employees";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        EnsureTable();
        LoadEmployees();
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

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 5
        };

        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        AddLabel(form, "Employee ID", 0, 0);
        txtEmployeeId.Dock = DockStyle.Fill;
        form.Controls.Add(txtEmployeeId, 1, 0);

        AddLabel(form, "Full Name", 2, 0);
        txtFullName.Dock = DockStyle.Fill;
        form.Controls.Add(txtFullName, 3, 0);

        AddLabel(form, "Department", 0, 1);
        txtDepartment.Dock = DockStyle.Fill;
        form.Controls.Add(txtDepartment, 1, 1);

        AddLabel(form, "Position", 2, 1);
        txtPosition.Dock = DockStyle.Fill;
        form.Controls.Add(txtPosition, 3, 1);


        AddLabel(form, "Immediate Supervisor", 0, 2);
        txtImmediateSupervisor.Dock = DockStyle.Fill;
        form.Controls.Add(txtImmediateSupervisor, 1, 2);

        AddLabel(form, "Supervisor Position", 2, 2);
        txtImmediateSupervisorPosition.Dock = DockStyle.Fill;
        form.Controls.Add(txtImmediateSupervisorPosition, 3, 2);

        var btnSave = new Button { Text = "Save / Update", Dock = DockStyle.Fill };
        btnSave.Click += (_, _) => SaveEmployee();

        var btnDelete = new Button { Text = "Delete Selected", Dock = DockStyle.Fill };
        btnDelete.Click += (_, _) => DeleteSelected();

        form.Controls.Add(btnSave, 1, 3);
        form.Controls.Add(btnDelete, 3, 3);

        var searchPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        searchPanel.Controls.Add(new Label
        {
            Text = "Search:",
            AutoSize = true,
            Padding = new Padding(0, 8, 5, 0)
        });

        txtSearch.Width = 300;
        txtSearch.PlaceholderText = "Search employee...";
        searchPanel.Controls.Add(txtSearch);

        var btnSearch = new Button
        {
            Text = "Search",
            Width = 100,
            Height = 32
        };
        btnSearch.Click += (_, _) => LoadEmployees();
        searchPanel.Controls.Add(btnSearch);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.CellDoubleClick += (_, _) => LoadSelectedToForm();

        main.Controls.Add(form, 0, 0);
        main.Controls.Add(searchPanel, 0, 1);
        main.Controls.Add(grid, 0, 2);

        Controls.Add(main);
    }

    private void AddLabel(TableLayoutPanel panel, string text, int col, int row)
    {
        panel.Controls.Add(new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        }, col, row);
    }

    private void EnsureTable()
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            CREATE TABLE IF NOT EXISTS employees (
                id INT AUTO_INCREMENT PRIMARY KEY,
                employee_id VARCHAR(50) NOT NULL UNIQUE,
                full_name VARCHAR(255) NOT NULL,
                department VARCHAR(255) NULL,
                position_title VARCHAR(255) NULL,
                is_active TINYINT(1) NOT NULL DEFAULT 1,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NULL
            );
        ", conn);

        cmd.ExecuteNonQuery();

        AddColumnIfMissing(conn, "immediate_supervisor_name", "VARCHAR(255) NULL");
        AddColumnIfMissing(conn, "immediate_supervisor_position", "VARCHAR(255) NULL");
    }

private void LoadEmployees()
{
    using var conn = Db.GetConnection();
    conn.Open();

    using var cmd = new MySqlCommand(@"
        SELECT
            employee_no,
            biometric_user_id,
            full_name,
            school_id,
            position_title,
            immediate_supervisor_name,
            immediate_supervisor_position,
            CASE
                WHEN is_active = 1 THEN 'Active'
                ELSE 'Inactive'
            END AS status
        FROM employees
        WHERE @search = ''
           OR employee_no LIKE CONCAT('%', @search, '%')
           OR biometric_user_id LIKE CONCAT('%', @search, '%')
           OR full_name LIKE CONCAT('%', @search, '%')
           OR school_id LIKE CONCAT('%', @search, '%')
        ORDER BY full_name ASC;
    ", conn);

    cmd.Parameters.AddWithValue("@search", txtSearch.Text.Trim());

    using var da = new MySqlDataAdapter(cmd);
    var dt = new DataTable();

    da.Fill(dt);

    grid.DataSource = dt;
}

private void SaveEmployee()
{
    var employeeNo = txtEmployeeId.Text.Trim();
    var fullName = txtFullName.Text.Trim();

    if (employeeNo == "" || fullName == "")
    {
        MessageBox.Show("Employee No and Full Name are required.");
        return;
    }

    using var conn = Db.GetConnection();
    conn.Open();

    using var cmd = new MySqlCommand(@"
        INSERT INTO employees
        (
            employee_no,
            biometric_user_id,
            school_id,
            full_name,
            position_title,
            immediate_supervisor_name,
            immediate_supervisor_position,
            is_active
        )
        VALUES
        (
            @employee_no,
            @biometric_user_id,
            @school_id,
            @full_name,
            @position_title,
            @immediate_supervisor_name,
            @immediate_supervisor_position,
            1
        )
        ON DUPLICATE KEY UPDATE
            school_id = VALUES(school_id),
            full_name = VALUES(full_name),
            position_title = VALUES(position_title),
            immediate_supervisor_name = VALUES(immediate_supervisor_name),
            immediate_supervisor_position = VALUES(immediate_supervisor_position),
            is_active = 1,
            updated_at = NOW();
    ", conn);

    cmd.Parameters.AddWithValue("@employee_no", employeeNo);

    // Required because biometric_user_id has no default value in your database.
    // For now, we keep it equal to employee_no when creating a new employee.
    // Existing employees will keep their current biometric_user_id because it is not updated below.
    cmd.Parameters.AddWithValue("@biometric_user_id", employeeNo);

    cmd.Parameters.AddWithValue("@school_id", txtDepartment.Text.Trim());
    cmd.Parameters.AddWithValue("@full_name", fullName);
    cmd.Parameters.AddWithValue("@position_title", txtPosition.Text.Trim());
    cmd.Parameters.AddWithValue("@immediate_supervisor_name", txtImmediateSupervisor.Text.Trim());
    cmd.Parameters.AddWithValue("@immediate_supervisor_position", txtImmediateSupervisorPosition.Text.Trim());

    cmd.ExecuteNonQuery();

    ClearForm();
    LoadEmployees();

    MessageBox.Show("Employee saved.");
}

private void DeleteSelected()
{
    if (grid.CurrentRow == null)
    {
        MessageBox.Show("Please select employee.");
        return;
    }

    var employeeNo =
        Convert.ToString(grid.CurrentRow.Cells["employee_no"].Value) ?? "";

    if (employeeNo == "")
        return;

    if (MessageBox.Show(
            "Deactivate selected employee?",
            "Confirm",
            MessageBoxButtons.YesNo
        ) != DialogResult.Yes)
        return;

    using var conn = Db.GetConnection();
    conn.Open();

    using var cmd = new MySqlCommand(@"
        UPDATE employees
        SET
            is_active = 0,
            updated_at = NOW()
        WHERE employee_no = @employee_no;
    ", conn);

    cmd.Parameters.AddWithValue("@employee_no", employeeNo);
    cmd.ExecuteNonQuery();

    LoadEmployees();

    MessageBox.Show("Employee deactivated.");
}

private void LoadSelectedToForm()
{
    if (grid.CurrentRow == null)
        return;

    txtEmployeeId.Text =
        Convert.ToString(grid.CurrentRow.Cells["employee_no"].Value) ?? "";

    txtFullName.Text =
        Convert.ToString(grid.CurrentRow.Cells["full_name"].Value) ?? "";

    txtDepartment.Text =
        Convert.ToString(grid.CurrentRow.Cells["school_id"].Value) ?? "";

    txtPosition.Text =
        Convert.ToString(grid.CurrentRow.Cells["position_title"].Value) ?? "";

    txtImmediateSupervisor.Text =
        Convert.ToString(grid.CurrentRow.Cells["immediate_supervisor_name"].Value) ?? "";

    txtImmediateSupervisorPosition.Text =
        Convert.ToString(grid.CurrentRow.Cells["immediate_supervisor_position"].Value) ?? "";
}

    private void ClearForm()
    {
        txtEmployeeId.Clear();
        txtFullName.Clear();
        txtDepartment.Clear();
        txtPosition.Clear();
        txtImmediateSupervisor.Clear();
        txtImmediateSupervisorPosition.Clear();
    }
    private static void AddColumnIfMissing(
    MySqlConnection conn,
    string columnName,
    string columnDefinition)
{
    using var checkCmd = new MySqlCommand(@"
        SELECT COUNT(*)
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'employees'
          AND COLUMN_NAME = @columnName;
    ", conn);

    checkCmd.Parameters.AddWithValue("@columnName", columnName);

    bool exists =
        Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

    if (exists)
        return;

    using var alterCmd = new MySqlCommand(
        $"ALTER TABLE employees ADD COLUMN {columnName} {columnDefinition};",
        conn);

    alterCmd.ExecuteNonQuery();
}
}