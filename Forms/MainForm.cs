using System;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using SchoolDTR.Forms;

namespace SchoolDTR.Services;

public class MainForm : Form
{
    private readonly TextBox txtLog = new();

    public MainForm()
    {
        Text = "City of Mati National High School (CMNHS) - 305680 DTR System";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
    }

    private void BuildUi()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(15)
        };

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "School Daily Time Record System",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2
        };

        for (int i = 0; i < 4; i++)
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        buttonPanel.Controls.Add(MakeButton("Device Setup", () => OpenForm("DeviceSetupForm")), 0, 0);
        buttonPanel.Controls.Add(MakeButton("Fetch Logs", FetchLogs), 1, 0);
        buttonPanel.Controls.Add(MakeButton("Manage Employees", () => OpenForm("EmployeeForm")), 2, 0);
        buttonPanel.Controls.Add(MakeButton("Events / Holidays", () => OpenForm("EventForm")), 3, 0);

        buttonPanel.Controls.Add(MakeButton("Generate DTR", GenerateDtr), 0, 1);
        buttonPanel.Controls.Add(MakeButton("View DTR", () => OpenForm("DtrViewerForm")), 1, 1);
        buttonPanel.Controls.Add(MakeButton("Raw Logs", () => OpenForm("RawLogsForm")), 2, 1);
        buttonPanel.Controls.Add(MakeButton("AO Print All", () => OpenForm("PrintAllDtrForm")), 3, 1);

        txtLog.Dock = DockStyle.Fill;
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.ReadOnly = true;
        txtLog.Font = new Font("Consolas", 10);

        main.Controls.Add(title, 0, 0);
        main.Controls.Add(buttonPanel, 0, 1);
        main.Controls.Add(txtLog, 0, 2);

        Controls.Add(main);

        Log("System ready.");
        // var hash = MachineFingerprintService.GetMachineHash();
        // Log("Machine Hash: " + hash);
        // MessageBox.Show(hash, "This Laptop Machine Hash");
    }

    private Button MakeButton(string text, Action action)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Height = 55
        };

        btn.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                var realError = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show(realError, text + " Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log(text + " Error: " + realError);
            }
        };

        return btn;
    }

    private async void FetchLogs()
    {
        try
        {
            var fromDate = DateTime.Today.AddDays(-30);
            var toDate = DateTime.Today;

            Log($"Fetching biometric logs from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}...");

            var result = await BiometricFetchService.FetchLogsAsync(fromDate, toDate);

            var createdEmployees = 0;

            if (result.Success)
            {
                createdEmployees = await EmployeeAutoCreateService.CreateMissingEmployeesFromRawLogsAsync();
            }

            if (result.Success)
            {
                Log($"Fetch completed. Total: {result.TotalLogs}, Inserted: {result.InsertedLogs}, Duplicates: {result.DuplicateLogs}, New employees: {createdEmployees}");

                    MessageBox.Show(
                        $"Fetch completed.\n\nTotal: {result.TotalLogs}\nInserted: {result.InsertedLogs}\nDuplicates: {result.DuplicateLogs}\nNew employees created: {createdEmployees}",
                        "Fetch Logs",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
            }
            else
            {
                Log("Fetch failed: " + result.Message);
                MessageBox.Show(result.Message, "Fetch Logs Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            var realError = ex.InnerException?.Message ?? ex.Message;
            Log("Fetch failed: " + realError);
            MessageBox.Show(realError, "Fetch Logs Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void GenerateDtr()
    {
        using var form = new GenerateDtrForm();
        form.ShowDialog();

        Log("Generate DTR form closed.");
    }

    private void OpenForm(string formClassName)
    {
        try
        {
            var fullName = $"SchoolDTR.Forms.{formClassName}";
            var type = Type.GetType(fullName);

            if (type == null)
            {
                MessageBox.Show($"{formClassName} does not exist yet.");
                Log($"{formClassName} not found.");
                return;
            }

            var form = Activator.CreateInstance(type) as Form;

            if (form == null)
            {
                MessageBox.Show($"{formClassName} is not a valid Form.");
                return;
            }

            form.ShowDialog();
            Log($"{formClassName} opened.");
        }
        catch (Exception ex)
        {
            var realError = ex.InnerException?.Message ?? ex.Message;

            MessageBox.Show(
                realError,
                $"{formClassName} Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            Log($"{formClassName} Error: {realError}");
        }
    }

    private void LogSafe(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(new Action(() => Log(message)));
            return;
        }

        Log(message);
    }

    private void Log(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
    }
}