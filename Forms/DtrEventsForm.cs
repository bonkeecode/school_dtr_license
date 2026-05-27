using MySqlConnector;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class DtrEventsForm : Form
{
    private DateTimePicker dtDateFrom = new DateTimePicker();
    private DateTimePicker dtDateTo = new DateTimePicker();
    private ComboBox cboEventType = new ComboBox();
    private TextBox txtTitle = new TextBox();
    private TextBox txtRemarks = new TextBox();
    private CheckedListBox chkEmployees = new CheckedListBox();
    private Button btnSelectAll = new Button();
    private Button btnClearAll = new Button();
    private Button btnSave = new Button();

    public DtrEventsForm()
    {
        Text = "DTR Events / Holidays - Bulk Assignment";
        Width = 760;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        LoadEmployees();
    }

    private void BuildUi()
    {
        Label lblDateFrom = new Label
        {
            Text = "Date From",
            Left = 30,
            Top = 30,
            Width = 120
        };

        dtDateFrom.Left = 170;
        dtDateFrom.Top = 25;
        dtDateFrom.Width = 200;
        dtDateFrom.Format = DateTimePickerFormat.Short;

        Label lblDateTo = new Label
        {
            Text = "Date To",
            Left = 400,
            Top = 30,
            Width = 100
        };

        dtDateTo.Left = 500;
        dtDateTo.Top = 25;
        dtDateTo.Width = 190;
        dtDateTo.Format = DateTimePickerFormat.Short;                                           

        Label lblType = new Label { Text = "Event Type", Left = 30, Top = 70, Width = 120 };
        cboEventType.Left = 170;
        cboEventType.Top = 65;
        cboEventType.Width = 200;
        cboEventType.DropDownStyle = ComboBoxStyle.DropDownList;
        cboEventType.Items.AddRange(new object[]
        {
            "HOLIDAY",
            "WFH",
            "TRAINING",
            "MEETING",
            "OFFICIAL BUSINESS",
            "SCHOOL ACTIVITY",
            "SUSPENSION",
            "OTHERS"
        });
        cboEventType.SelectedIndex = 0;

        Label lblTitle = new Label { Text = "Title", Left = 30, Top = 110, Width = 120 };
        txtTitle.Left = 170;
        txtTitle.Top = 105;
        txtTitle.Width = 520;

        Label lblRemarks = new Label { Text = "Remarks", Left = 30, Top = 150, Width = 120 };
        txtRemarks.Left = 170;
        txtRemarks.Top = 145;
        txtRemarks.Width = 520;

        Label lblEmployees = new Label { Text = "Employees", Left = 30, Top = 195, Width = 120 };
        chkEmployees.Left = 170;
        chkEmployees.Top = 195;
        chkEmployees.Width = 520;
        chkEmployees.Height = 280;
        chkEmployees.CheckOnClick = true;

        btnSelectAll.Text = "Select All";
        btnSelectAll.Left = 170;
        btnSelectAll.Top = 490;
        btnSelectAll.Width = 120;
        btnSelectAll.Click += (_, _) => SetAllEmployees(true);

        btnClearAll.Text = "Clear All";
        btnClearAll.Left = 300;
        btnClearAll.Top = 490;
        btnClearAll.Width = 120;
        btnClearAll.Click += (_, _) => SetAllEmployees(false);

        btnSave.Text = "Save Assignment";
        btnSave.Left = 520;
        btnSave.Top = 490;
        btnSave.Width = 170;
        btnSave.Height = 35;
        btnSave.Click += async (_, _) => await SaveEventAsync();

       Controls.AddRange(new Control[]
        {
            lblDateFrom,
            dtDateFrom,
            lblDateTo,
            dtDateTo,
            lblType, cboEventType,
            lblTitle, txtTitle,
            lblRemarks, txtRemarks,
            lblEmployees, chkEmployees,
            btnSelectAll, btnClearAll, btnSave
        });
    }

    private async void LoadEmployees()
    {
        chkEmployees.Items.Clear();

        using var conn = Db.GetConnection();
        await conn.OpenAsync();

        string sql = @"
            SELECT employee_no, full_name
            FROM employees
            WHERE school_id = @school_id
              AND is_active = 1
            ORDER BY full_name;
        ";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@school_id", AppConfig.SchoolCode);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            string employeeNo = reader["employee_no"].ToString() ?? "";
            string fullName = reader["full_name"].ToString() ?? "";
            chkEmployees.Items.Add(new EmployeeCheckItem(employeeNo, fullName), false);
        }
    }

    private void SetAllEmployees(bool isChecked)
    {
        for (int i = 0; i < chkEmployees.Items.Count; i++)
            chkEmployees.SetItemChecked(i, isChecked);
    }

    private async Task SaveEventAsync()
    {
        string eventType = cboEventType.Text.Trim();
        string title = txtTitle.Text.Trim();
        string remarks = txtRemarks.Text.Trim();

        if (title == "")
        {
            MessageBox.Show("Please enter event title.");
            return;
        }

        if (chkEmployees.CheckedItems.Count == 0)
        {
            MessageBox.Show("Please select at least one employee.");
            return;
        }

        using var conn = Db.GetConnection();
        await conn.OpenAsync();

        using var tx = await conn.BeginTransactionAsync();

        try
        {
            string insertEventSql = @"
                INSERT INTO dtr_events
                (
                    school_id,
                    date_from,
                    date_to,
                    event_type,
                    event_title,
                    remarks
                )
                VALUES
                (
                    @school_id,
                    @date_from,
                    @date_to,
                    @event_type,
                    @event_title,
                    @remarks
                );
            ";

            using var eventCmd = new MySqlCommand(insertEventSql, conn, (MySqlTransaction)tx);
            eventCmd.Parameters.AddWithValue("@school_id", AppConfig.SchoolCode);
            if (dtDateTo.Value.Date < dtDateFrom.Value.Date)
{
    MessageBox.Show("Date To cannot be earlier than Date From.");
    return;
}

eventCmd.Parameters.AddWithValue("@date_from", dtDateFrom.Value.Date);
eventCmd.Parameters.AddWithValue("@date_to", dtDateTo.Value.Date);
            eventCmd.Parameters.AddWithValue("@event_type", eventType);
            eventCmd.Parameters.AddWithValue("@event_title", title);
            eventCmd.Parameters.AddWithValue("@remarks", remarks);

            long eventId = Convert.ToInt64(await eventCmd.ExecuteScalarAsync());

            foreach (var checkedItem in chkEmployees.CheckedItems)
            {
                var emp = (EmployeeCheckItem)checkedItem;

                string assignSql = @"
                    INSERT IGNORE INTO dtr_event_assignments
                    (event_id, employee_no)
                    VALUES
                    (@event_id, @employee_no);
                ";

                using var assignCmd = new MySqlCommand(assignSql, conn, (MySqlTransaction)tx);
                assignCmd.Parameters.AddWithValue("@event_id", eventId);
                assignCmd.Parameters.AddWithValue("@employee_no", emp.EmployeeNo);
                await assignCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            MessageBox.Show("DTR event saved. Click Generate DTR again to apply it.");
            Close();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            MessageBox.Show("Failed to save event: " + ex.Message);
        }
    }

    private class EmployeeCheckItem
    {
        public string EmployeeNo { get; }
        public string FullName { get; }

        public EmployeeCheckItem(string employeeNo, string fullName)
        {
            EmployeeNo = employeeNo;
            FullName = fullName;
        }

        public override string ToString()
        {
            return $"{FullName} ({EmployeeNo})";
        }
    }
}