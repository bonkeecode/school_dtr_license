using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;
using SchoolDTR.Services;
using System.Collections.Generic;
namespace SchoolDTR.Forms;

public class EventForm : Form
{
    private readonly TextBox txtTitle = new();
    private readonly DateTimePicker dtFrom = new();
    private readonly DateTimePicker dtTo = new();
    private readonly ComboBox cboType = new();
    private readonly TextBox txtRemarks = new();

    private readonly TextBox txtEmployeeSearch = new();
    private readonly CheckedListBox employeeList = new();

    private readonly DataGridView grid = new();

    public EventForm()
    {
        Text = "Events / Holidays";
        Width = 1100;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        EnsureTables();
        LoadEmployeesForAssignment();
        LoadEvents();
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

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4
        };

        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        AddLabel(form, "Event Title", 0, 0);
        txtTitle.Dock = DockStyle.Fill;
        form.Controls.Add(txtTitle, 1, 0);
        form.SetColumnSpan(txtTitle, 3);

        AddLabel(form, "Date From", 0, 1);
        dtFrom.Dock = DockStyle.Fill;
        dtFrom.Format = DateTimePickerFormat.Short;
        form.Controls.Add(dtFrom, 1, 1);

        AddLabel(form, "Date To", 2, 1);
        dtTo.Dock = DockStyle.Fill;
        dtTo.Format = DateTimePickerFormat.Short;
        form.Controls.Add(dtTo, 3, 1);

        AddLabel(form, "Event Type", 0, 2);
        cboType.Dock = DockStyle.Fill;
        cboType.DropDownStyle = ComboBoxStyle.DropDownList;
        cboType.Items.AddRange(new object[]
        {
            "Holiday",
            "School Event",
            "Work From Home",
            "Travel",
            "Training",
            "Seminar",
            "Suspension",
            "Weekend Override",
            "Other"
        });
        cboType.SelectedIndex = 0;
        form.Controls.Add(cboType, 1, 2);

        AddLabel(form, "Remarks", 2, 2);
        txtRemarks.Dock = DockStyle.Fill;
        form.Controls.Add(txtRemarks, 3, 2);

        var btnSave = new Button
        {
            Text = "Save Event and Assign",
            Dock = DockStyle.Fill,
            Height = 40
        };
        btnSave.Click += (_, _) => SaveEvent();

        var btnDelete = new Button
        {
            Text = "Delete Selected Event",
            Dock = DockStyle.Fill,
            Height = 40
        };
        btnDelete.Click += (_, _) => DeleteSelected();

        form.Controls.Add(btnSave, 1, 3);
        form.Controls.Add(btnDelete, 3, 3);

        var assignmentPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 3
        };

        assignmentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        assignmentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        assignmentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

        assignmentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        assignmentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        assignmentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        txtEmployeeSearch.Dock = DockStyle.Fill;
        txtEmployeeSearch.PlaceholderText = "Search employee no, biometric id, name, or school...";
        txtEmployeeSearch.TextChanged += (_, _) => LoadEmployeesForAssignment();

        var btnCheckAll = new Button
        {
            Text = "Check All Visible",
            Dock = DockStyle.Fill
        };
        btnCheckAll.Click += (_, _) =>
        {
            for (int i = 0; i < employeeList.Items.Count; i++)
                employeeList.SetItemChecked(i, true);
        };

        var btnUncheckAll = new Button
        {
            Text = "Uncheck All",
            Dock = DockStyle.Fill
        };
        btnUncheckAll.Click += (_, _) =>
        {
            for (int i = 0; i < employeeList.Items.Count; i++)
                employeeList.SetItemChecked(i, false);
        };

        var lblAssign = new Label
        {
            Text = "Assign event to employees",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        employeeList.Dock = DockStyle.Fill;
        employeeList.CheckOnClick = true;

        assignmentPanel.Controls.Add(lblAssign, 0, 0);
        assignmentPanel.SetColumnSpan(lblAssign, 3);

        assignmentPanel.Controls.Add(txtEmployeeSearch, 0, 1);
        assignmentPanel.Controls.Add(btnCheckAll, 1, 1);
        assignmentPanel.Controls.Add(btnUncheckAll, 2, 1);

        assignmentPanel.Controls.Add(employeeList, 0, 2);
        assignmentPanel.SetColumnSpan(employeeList, 3);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        main.Controls.Add(form, 0, 0);
        main.Controls.Add(assignmentPanel, 0, 1);
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

    private void EnsureTables()
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var cmdEvents = new MySqlCommand(@"
            CREATE TABLE IF NOT EXISTS dtr_events (
                id INT AUTO_INCREMENT PRIMARY KEY,
                school_id VARCHAR(20) NOT NULL DEFAULT 'ASSIGNED',
                event_title VARCHAR(255) NOT NULL,
                date_from DATE NOT NULL,
                date_to DATE NOT NULL,
                event_type VARCHAR(50) NOT NULL,
                remarks VARCHAR(255) NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ", conn);

        cmdEvents.ExecuteNonQuery();

        using var cmdAssignments = new MySqlCommand(@"
            CREATE TABLE IF NOT EXISTS dtr_event_assignments (
                id INT AUTO_INCREMENT PRIMARY KEY,
                event_id INT NOT NULL,
                employee_no VARCHAR(50) NOT NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UNIQUE KEY uq_event_employee (event_id, employee_no)
            );
        ", conn);

        cmdAssignments.ExecuteNonQuery();
    }

    private void LoadEmployeesForAssignment()
    {
        var checkedEmployeeNos = new HashSet<string>();

        foreach (var checkedItem in employeeList.CheckedItems)
        {
            if (checkedItem is EmployeeCheckItem item)
                checkedEmployeeNos.Add(item.EmployeeNo);
        }

        employeeList.Items.Clear();

        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT
                employee_no,
                biometric_user_id,
                full_name,
                school_id,
                position_title
            FROM employees
            WHERE is_active = 1
              AND (
                    @search = ''
                    OR employee_no LIKE CONCAT('%', @search, '%')
                    OR biometric_user_id LIKE CONCAT('%', @search, '%')
                    OR full_name LIKE CONCAT('%', @search, '%')
                    OR school_id LIKE CONCAT('%', @search, '%')
                  )
            ORDER BY full_name ASC;
        ", conn);

        cmd.Parameters.AddWithValue("@search", txtEmployeeSearch.Text.Trim());

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            string employeeNo = Convert.ToString(reader["employee_no"]) ?? "";
            string biometricId = Convert.ToString(reader["biometric_user_id"]) ?? "";
            string fullName = Convert.ToString(reader["full_name"]) ?? "";
            string schoolId = Convert.ToString(reader["school_id"]) ?? "";
            string position = Convert.ToString(reader["position_title"]) ?? "";

            var item = new EmployeeCheckItem
            {
                EmployeeNo = employeeNo,
                DisplayName = $"{employeeNo} | Bio: {biometricId} | {fullName} | {schoolId} | {position}"
            };

            int index = employeeList.Items.Add(item);

            if (checkedEmployeeNos.Contains(employeeNo))
                employeeList.SetItemChecked(index, true);
        }
    }

    private void LoadEvents()
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var da = new MySqlDataAdapter(@"
            SELECT
                e.id,
                e.event_title,
                e.date_from,
                e.date_to,
                e.event_type,
                COUNT(a.id) AS assigned_employees,
                e.remarks
            FROM dtr_events e
            LEFT JOIN dtr_event_assignments a
                ON a.event_id = e.id
            GROUP BY
                e.id,
                e.event_title,
                e.date_from,
                e.date_to,
                e.event_type,
                e.remarks
            ORDER BY e.date_from DESC, e.id DESC;
        ", conn);

        var dt = new DataTable();
        da.Fill(dt);
        grid.DataSource = dt;
    }

    private void SaveEvent()
    {
        var title = txtTitle.Text.Trim();

        if (title == "")
        {
            MessageBox.Show("Please enter event title.");
            return;
        }

        if (dtTo.Value.Date < dtFrom.Value.Date)
        {
            MessageBox.Show("Date To cannot be earlier than Date From.");
            return;
        }

        if (employeeList.CheckedItems.Count == 0)
        {
            MessageBox.Show("Please select at least one employee.");
            return;
        }

        using var conn = Db.GetConnection();
        conn.Open();

        using var tx = conn.BeginTransaction();

        try
        {
            using var cmd = new MySqlCommand(@"
                INSERT INTO dtr_events
                    (school_id, event_title, date_from, date_to, event_type, remarks)
                VALUES
                    ('ASSIGNED', @title, @from, @to, @type, @remarks);
                SELECT LAST_INSERT_ID();
            ", conn, tx);

            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@from", dtFrom.Value.Date);
            cmd.Parameters.AddWithValue("@to", dtTo.Value.Date);
            cmd.Parameters.AddWithValue("@type", cboType.Text);
            cmd.Parameters.AddWithValue("@remarks", txtRemarks.Text.Trim());

            int eventId = Convert.ToInt32(cmd.ExecuteScalar());

            foreach (var checkedItem in employeeList.CheckedItems)
            {
                if (checkedItem is not EmployeeCheckItem item)
                    continue;

                using var assignCmd = new MySqlCommand(@"
                    INSERT IGNORE INTO dtr_event_assignments
                        (event_id, employee_no)
                    VALUES
                        (@event_id, @employee_no);
                ", conn, tx);

                assignCmd.Parameters.AddWithValue("@event_id", eventId);
                assignCmd.Parameters.AddWithValue("@employee_no", item.EmployeeNo);

                assignCmd.ExecuteNonQuery();
            }

            tx.Commit();

            txtTitle.Clear();
            txtRemarks.Clear();
            dtFrom.Value = DateTime.Today;
            dtTo.Value = DateTime.Today;
            cboType.SelectedIndex = 0;

            for (int i = 0; i < employeeList.Items.Count; i++)
                employeeList.SetItemChecked(i, false);

            LoadEvents();

            MessageBox.Show("Event saved and assigned to selected employees.");
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private void DeleteSelected()
    {
        if (grid.CurrentRow == null)
        {
            MessageBox.Show("Please select an event.");
            return;
        }

        var id = Convert.ToInt32(grid.CurrentRow.Cells["id"].Value);

        if (MessageBox.Show(
                "Delete selected event and its employee assignments?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            ) != DialogResult.Yes)
            return;

        using var conn = Db.GetConnection();
        conn.Open();

        using var tx = conn.BeginTransaction();

        try
        {
            using var deleteAssignments = new MySqlCommand(@"
                DELETE FROM dtr_event_assignments
                WHERE event_id = @id;
            ", conn, tx);

            deleteAssignments.Parameters.AddWithValue("@id", id);
            deleteAssignments.ExecuteNonQuery();

            using var deleteEvent = new MySqlCommand(@"
                DELETE FROM dtr_events
                WHERE id = @id;
            ", conn, tx);

            deleteEvent.Parameters.AddWithValue("@id", id);
            deleteEvent.ExecuteNonQuery();

            tx.Commit();

            LoadEvents();

            MessageBox.Show("Event deleted.");
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private class EmployeeCheckItem
    {
        public string EmployeeNo { get; set; } = "";
        public string DisplayName { get; set; } = "";

        public override string ToString()
        {
            return DisplayName;
        }
    }
}