using System;
using System.Drawing;
using System.Windows.Forms;
using SchoolDTR.Forms;

namespace SchoolDTR.Services;

public class MainForm : Form
{
    private readonly TextBox txtLog = new();

    public MainForm()
    {
        Text = "City of Mati National High School (CMNHS) - 305680 DTR System";
        Width = 1150;
        Height = 780;
        MinimumSize = new Size(1050, 720);
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
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 350));
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
            RowCount = 4
        };

        buttonPanel.ColumnStyles.Clear();
        for (int i = 0; i < 4; i++)
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        buttonPanel.RowStyles.Clear();
        for (int i = 0; i < 4; i++)
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));

        buttonPanel.Controls.Add(MakeButton("Device Setup", () => OpenForm("DeviceSetupForm")), 0, 0);
        buttonPanel.Controls.Add(MakeButton("Fetch Logs", FetchLogs), 1, 0);
        buttonPanel.Controls.Add(MakeButton("Manage Employees", () => OpenForm("EmployeeForm")), 2, 0);
        buttonPanel.Controls.Add(MakeButton("Events / Holidays", () => OpenForm("EventForm")), 3, 0);

        buttonPanel.Controls.Add(MakeButton("Generate DTR", GenerateDtr), 0, 1);
        buttonPanel.Controls.Add(MakeButton("View DTR", () => OpenForm("DtrViewerForm")), 1, 1);
        buttonPanel.Controls.Add(MakeButton("Raw Logs", () => OpenForm("RawLogsForm")), 2, 1);
        buttonPanel.Controls.Add(MakeButton("AO Print All", () => OpenForm("PrintAllDtrForm")), 3, 1);

        buttonPanel.Controls.Add(MakeButton("Settings", OpenSettings), 0, 2);
        buttonPanel.Controls.Add(MakeButton("Health Check", OpenHealthCheck), 1, 2);
        buttonPanel.Controls.Add(MakeButton("Backup DB", BackupDatabase), 2, 2);
        buttonPanel.Controls.Add(MakeButton("Audit Logs", OpenAuditLogs), 3, 2);

        buttonPanel.Controls.Add(MakeButton("Mapping Check", OpenMappingCheck), 0, 3);

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
    }

    private Button MakeButton(string text, Action action)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(5),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
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
            RunAutoBackup("Before fetching logs");

            using var dlg = new FetchLogsDateRangeForm();

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            var fromDate = dlg.DateFrom;
            var toDate = dlg.DateTo;

            Log($"Fetching biometric logs from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}...");

            var result = await BiometricFetchService.FetchLogsAsync(fromDate, toDate);

            AuditLogService.Log(
                "FETCH_LOGS",
                $"Fetched logs from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}. " +
                $"Total: {result.TotalLogs}, Inserted: {result.InsertedLogs}, Duplicates: {result.DuplicateLogs}."
            );

            var createdEmployees = 0;

            if (result.Success)
                createdEmployees = await EmployeeAutoCreateService.CreateMissingEmployeesFromRawLogsAsync();

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
        RunAutoBackup("Before generating DTR");

        using var form = new GenerateDtrForm();
        form.ShowDialog(this);

        AuditLogService.Log("GENERATE_DTR", "Generated DTR.");
        Log("Generate DTR form closed.");
    }

    private void OpenForm(string formClassName)
    {
        var fullName = $"SchoolDTR.Forms.{formClassName}";
        var type = Type.GetType(fullName);

        if (type == null)
        {
            MessageBox.Show($"{formClassName} does not exist yet.");
            Log($"{formClassName} not found.");
            return;
        }

        using var form = Activator.CreateInstance(type) as Form;

        if (form == null)
        {
            MessageBox.Show($"{formClassName} is not a valid Form.");
            return;
        }

        form.ShowDialog(this);
        Log($"{formClassName} opened.");
    }

    private void OpenSettings()
    {
        using var f = new SettingsForm();
        f.ShowDialog(this);
    }

    private void OpenHealthCheck()
    {
        using var f = new HealthCheckForm();
        f.ShowDialog(this);
    }

    private void BackupDatabase()
    {
        var path = DatabaseBackupService.Backup();

        AuditLogService.Log("BACKUP_DB", "Database backup created: " + path);

        MessageBox.Show(
            "Database backup completed:\n\n" + path,
            "Backup Database",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    private void OpenAuditLogs()
    {
        using var f = new AuditLogsForm();
        f.ShowDialog(this);
    }

    private void OpenMappingCheck()
    {
        using var f = new EmployeeMappingCheckForm();
        f.ShowDialog(this);
    }

    private void RunAutoBackup(string reason)
    {
        try
        {
            var path = DatabaseBackupService.Backup();

            AuditLogService.Log(
                "AUTO_BACKUP",
                $"{reason}. Backup created: {path}"
            );
        }
        catch (Exception ex)
        {
            AuditLogService.Log(
                "AUTO_BACKUP_FAILED",
                $"{reason}. Backup failed: {ex.Message}"
            );
        }
    }

    private void Log(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
    }
}