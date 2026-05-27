using MySqlConnector;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class DtrPrintForm : Form
{
    private ComboBox cboEmployee = new ComboBox();
    private DateTimePicker dtMonth = new DateTimePicker();
    private CheckBox chkPrintAll = new CheckBox();
    private Button btnPreview = new Button();
    private Button btnPrint = new Button();

    public DtrPrintForm()
    {
        Text = "Print CSC Form 48";
        Width = 520;
        Height = 260;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        LoadEmployees();
    }

    private void BuildUi()
    {
        Label lblMonth = new Label { Text = "Month", Left = 30, Top = 30, Width = 100 };
        dtMonth.Left = 150;
        dtMonth.Top = 25;
        dtMonth.Width = 180;
        dtMonth.Format = DateTimePickerFormat.Custom;
        dtMonth.CustomFormat = "MMMM yyyy";
        dtMonth.ShowUpDown = true;

        Label lblEmployee = new Label { Text = "Employee", Left = 30, Top = 75, Width = 100 };
        cboEmployee.Left = 150;
        cboEmployee.Top = 70;
        cboEmployee.Width = 300;
        cboEmployee.DropDownStyle = ComboBoxStyle.DropDownList;

        chkPrintAll.Text = "Print all active employees";
        chkPrintAll.Left = 150;
        chkPrintAll.Top = 110;
        chkPrintAll.Width = 250;
        chkPrintAll.CheckedChanged += (_, _) => cboEmployee.Enabled = !chkPrintAll.Checked;

        btnPreview.Text = "Preview";
        btnPreview.Left = 150;
        btnPreview.Top = 155;
        btnPreview.Width = 120;
        btnPreview.Height = 35;
        btnPreview.Click += (_, _) => Print(preview: true);

        btnPrint.Text = "Print";
        btnPrint.Left = 290;
        btnPrint.Top = 155;
        btnPrint.Width = 120;
        btnPrint.Height = 35;
        btnPrint.Click += (_, _) => Print(preview: false);

        Controls.AddRange(new Control[]
        {
            lblMonth,
            dtMonth,
            lblEmployee,
            cboEmployee,
            chkPrintAll,
            btnPreview,
            btnPrint
        });
    }

    private async void LoadEmployees()
    {
        cboEmployee.Items.Clear();

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
            cboEmployee.Items.Add(new EmployeeItem(employeeNo, fullName));
        }

        if (cboEmployee.Items.Count > 0)
            cboEmployee.SelectedIndex = 0;
    }

    private void Print(bool preview)
    {
        List<string> employeeNos = new();

        if (chkPrintAll.Checked)
        {
            foreach (var item in cboEmployee.Items)
            {
                if (item is EmployeeItem emp)
                    employeeNos.Add(emp.EmployeeNo);
            }
        }
        else
        {
            if (cboEmployee.SelectedItem is not EmployeeItem emp)
            {
                MessageBox.Show("Please select employee.");
                return;
            }

            employeeNos.Add(emp.EmployeeNo);
        }

        if (employeeNos.Count == 0)
        {
            MessageBox.Show("No employee selected.");
            return;
        }

        // var printer = new CscForm48Printer(dtMonth.Value, employeeNos);
        // printer.Print(preview);
    }

    private class EmployeeItem
    {
        public string EmployeeNo { get; }
        public string FullName { get; }

        public EmployeeItem(string employeeNo, string fullName)
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