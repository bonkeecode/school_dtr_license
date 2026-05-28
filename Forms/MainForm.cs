using System;
using System.Drawing;
using System.Windows.Forms;
using SchoolDTR.Forms;

namespace SchoolDTR.Services;

public class MainForm : Form
{
    private readonly TextBox txtLog = new();

    private readonly Button btnDeviceSetup = new();
    private readonly Button btnFetch = new();
    private readonly Button btnEmployees = new();
    private readonly Button btnEvents = new();
    private readonly Button btnGenerate = new();
    private readonly Button btnViewDtr = new();
    private readonly Button btnRawLogs = new();
    private readonly Button btnPrintAll = new();
    private readonly Button btnSettings = new();
    private readonly Button btnHealthCheck = new();
    private readonly Button btnBackup = new();
    private readonly Button btnAuditLogs = new();
    private readonly Button btnMappingCheck = new();

    private Label lblClock = new();
    private readonly System.Windows.Forms.Timer clockTimer = new();

    private readonly Color depedBlue = Color.FromArgb(15, 45, 95);
    private readonly Color depedBlueLight = Color.FromArgb(30, 90, 180);
    private readonly Color depedRed = Color.FromArgb(185, 28, 28);
    private readonly Color bgGray = Color.FromArgb(243, 244, 246);
    private readonly Color textDark = Color.FromArgb(31, 41, 55);
    private readonly Color textMuted = Color.FromArgb(75, 85, 99);

    public MainForm()
    {
        Text = "City of Mati National High School (CMNHS) - 305680 DTR System";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1200, 720);
        BackColor = bgGray;

        BuildUi();
        WireButtonActions();

        Log("System ready.");
    }

    private void BuildUi()
    {
        Controls.Clear();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = bgGray
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(BuildMainDashboard(), 1, 0);

        Controls.Add(root);
        StartClock();
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = depedBlue,
            Padding = new Padding(18)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 9,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 115));
        for (var i = 1; i <= 7; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var brand = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var logo = new Label
        {
            Text = "🏫",
            Font = new Font("Segoe UI Emoji", 30, FontStyle.Regular),
            ForeColor = Color.White,
            Location = new Point(0, 12),
            Size = new Size(52, 52),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var title = new Label
        {
            Text = "School DTR System",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(68, 18),
            Size = new Size(175, 24),
            AutoEllipsis = true
        };

        var subtitle = new Label
        {
            Text = "Daily Time Record",
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.FromArgb(210, 220, 235),
            Location = new Point(70, 44),
            Size = new Size(170, 20),
            AutoEllipsis = true
        };

        brand.Controls.Add(logo);
        brand.Controls.Add(title);
        brand.Controls.Add(subtitle);

        layout.Controls.Add(brand, 0, 0);
        layout.Controls.Add(CreateMenuButton("📊  Dashboard", true), 0, 1);
        layout.Controls.Add(CreateMenuButton("👥  Employee Management"), 0, 2);
        layout.Controls.Add(CreateMenuButton("🕒  DTR / Time Logs"), 0, 3);
        layout.Controls.Add(CreateMenuButton("📝  Leave & Accomplishments"), 0, 4);
        layout.Controls.Add(CreateMenuButton("🔐  Biometric Device Management"), 0, 5);
        layout.Controls.Add(CreateMenuButton("📈  Reports & Analytics"), 0, 6);
        layout.Controls.Add(CreateMenuButton("⚙️  System Settings"), 0, 7);

        sidebar.Controls.Add(layout);
        return sidebar;
    }

    private Button CreateMenuButton(string text, bool active = false)
    {
        return new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = active ? depedBlueLight : depedBlue,
            Cursor = Cursors.Hand,
            FlatAppearance =
            {
                BorderSize = 0,
                MouseOverBackColor = Color.FromArgb(25, 75, 145),
                MouseDownBackColor = Color.FromArgb(20, 65, 125)
            }
        };
    }

    private Panel BuildMainDashboard()
    {
        var main = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = bgGray,
            Padding = new Padding(28)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 115));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));

        layout.Controls.Add(BuildTopBar(), 0, 0);
        layout.Controls.Add(BuildWelcomeCard(), 0, 1);
        layout.Controls.Add(BuildStatCards(), 0, 2);
        layout.Controls.Add(BuildActionArea(), 0, 3);
        layout.Controls.Add(BuildLogArea(), 0, 4);

        main.Controls.Add(layout);
        return main;
    }

    private Panel BuildTopBar()
    {
        var top = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var title = new Label
        {
            Text = "Dashboard",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = textDark,
            AutoSize = true,
            Location = new Point(0, 8)
        };

        lblClock = new Label
        {
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(55, 65, 81),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(460, 36),
            Location = new Point(top.Width - 460, 14)
        };

        top.Resize += (_, _) =>
        {
            lblClock.Location = new Point(top.Width - lblClock.Width, 14);
        };

        top.Controls.Add(title);
        top.Controls.Add(lblClock);
        return top;
    }

    private Panel BuildWelcomeCard()
    {
        var card = CreateCard();

        var title = new Label
        {
            Text = "Welcome to School Daily Time Record System",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = textDark,
            AutoSize = true,
            Location = new Point(24, 20)
        };

        var desc = new Label
        {
            Text = "Manage employee attendance, biometric logs, CSC Form 48 printing, backups, audit logs, and ZKTeco K14 synchronization in one dashboard.",
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = textMuted,
            AutoSize = true,
            Location = new Point(26, 58)
        };

        card.Controls.Add(title);
        card.Controls.Add(desc);
        return card;
    }

    private TableLayoutPanel BuildStatCards()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 12)
        };

        for (var i = 0; i < 4; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        grid.Controls.Add(CreateStatCard("👥", "Employees", "Active Personnel", "Blue"), 0, 0);
        grid.Controls.Add(CreateStatCard("🕒", "Time Logs", "Today's Records", "Blue"), 1, 0);
        grid.Controls.Add(CreateStatCard("⚠️", "Pending Issues", "Logs to Review", "Red"), 2, 0);
        grid.Controls.Add(CreateStatCard("🖨️", "Reports", "Ready to Print", "Blue"), 3, 0);

        return grid;
    }

    private Panel CreateStatCard(string icon, string title, string subtitle, string accent)
    {
        var card = CreateCard();
        card.Margin = new Padding(0, 0, 16, 0);
        card.Padding = new Padding(0);

        var iconLabel = new Label
        {
            Text = icon,
            Font = new Font("Segoe UI Emoji", 24, FontStyle.Regular),
            ForeColor = Color.FromArgb(17, 24, 39),
            Location = new Point(22, 30),
            Size = new Size(52, 52),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = textDark,
            Location = new Point(92, 32),
            Size = new Size(210, 24),
            AutoEllipsis = true
        };

        var subLabel = new Label
        {
            Text = subtitle,
            Font = new Font("Segoe UI", 8, FontStyle.Regular),
            ForeColor = accent == "Red" ? depedRed : textMuted,
            Location = new Point(94, 58),
            Size = new Size(210, 20),
            AutoEllipsis = true
        };

        card.Controls.Add(iconLabel);
        card.Controls.Add(titleLabel);
        card.Controls.Add(subLabel);
        return card;
    }

    private Panel BuildActionArea()
    {
        var card = CreateCard();
        card.Padding = new Padding(24);

        var title = new Label
        {
            Text = "Quick Actions",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = textDark,
            AutoSize = true,
            Location = new Point(24, 22)
        };

        var subtitle = new Label
        {
            Text = "Frequently used DTR, biometric, reporting, and maintenance tools.",
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = textMuted,
            AutoSize = true,
            Location = new Point(26, 50)
        };

        var actions = new FlowLayoutPanel
        {
            Location = new Point(24, 85),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            Size = new Size(card.Width - 48, card.Height - 105),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        card.Resize += (_, _) =>
        {
            actions.Size = new Size(card.Width - 48, card.Height - 105);
        };

        ConfigureActionButtons(actions);

        card.Controls.Add(title);
        card.Controls.Add(subtitle);
        card.Controls.Add(actions);

        return card;
    }

    private void ConfigureActionButtons(FlowLayoutPanel actions)
    {
        btnDeviceSetup.Text = "Biometric Setup";
        btnFetch.Text = "Fetch Logs";
        btnEmployees.Text = "Employees";
        btnEvents.Text = "Events / Holidays";
        btnGenerate.Text = "Generate DTR";
        btnViewDtr.Text = "View DTR";
        btnRawLogs.Text = "Raw Logs";
        btnPrintAll.Text = "AO Print All";
        btnSettings.Text = "Settings";
        btnHealthCheck.Text = "Health Check";
        btnBackup.Text = "Backup DB";
        btnAuditLogs.Text = "Audit Logs";
        btnMappingCheck.Text = "Mapping Check";

        StylePrimaryButton(btnDeviceSetup, "🔐");
        StylePrimaryButton(btnFetch, "⬇️");
        StylePrimaryButton(btnEmployees, "👥");
        StylePrimaryButton(btnEvents, "📅");
        StylePrimaryButton(btnGenerate, "⚙️");
        StylePrimaryButton(btnViewDtr, "📄");
        StylePrimaryButton(btnRawLogs, "🧾");
        StylePrimaryButton(btnPrintAll, "🖨️");
        StylePrimaryButton(btnSettings, "⚙️");
        StylePrimaryButton(btnHealthCheck, "🩺");
        StylePrimaryButton(btnBackup, "💾");
        StylePrimaryButton(btnAuditLogs, "📜");
        StylePrimaryButton(btnMappingCheck, "🔎");

        actions.Controls.Add(btnDeviceSetup);
        actions.Controls.Add(btnFetch);
        actions.Controls.Add(btnEmployees);
        actions.Controls.Add(btnEvents);
        actions.Controls.Add(btnGenerate);
        actions.Controls.Add(btnViewDtr);
        actions.Controls.Add(btnRawLogs);
        actions.Controls.Add(btnPrintAll);
        actions.Controls.Add(btnSettings);
        actions.Controls.Add(btnHealthCheck);
        actions.Controls.Add(btnBackup);
        actions.Controls.Add(btnAuditLogs);
        actions.Controls.Add(btnMappingCheck);
    }

    private Panel BuildLogArea()
    {
        var card = CreateCard();
        card.Margin = new Padding(0);
        card.Padding = new Padding(16);

        var title = new Label
        {
            Text = "Activity Log",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = textDark,
            Dock = DockStyle.Top,
            Height = 26
        };

        txtLog.Dock = DockStyle.Fill;
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.ReadOnly = true;
        txtLog.BorderStyle = BorderStyle.None;
        txtLog.BackColor = Color.White;
        txtLog.ForeColor = Color.FromArgb(55, 65, 81);
        txtLog.Font = new Font("Consolas", 9);

        card.Controls.Add(txtLog);
        card.Controls.Add(title);
        return card;
    }

    private Panel CreateCard()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(18),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private void StylePrimaryButton(Button button, string icon)
    {
        button.Width = 170;
        button.Height = 62;
        button.Margin = new Padding(0, 0, 14, 14);
        button.Text = $"{icon}  {button.Text}";
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.ForeColor = Color.White;
        button.BackColor = depedBlueLight;
        button.FlatStyle = FlatStyle.Flat;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 75, 160);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 60, 130);
    }

    private void StartClock()
    {
        clockTimer.Stop();
        clockTimer.Tick -= ClockTimer_Tick;
        clockTimer.Interval = 1000;
        clockTimer.Tick += ClockTimer_Tick;
        ClockTimer_Tick(this, EventArgs.Empty);
        clockTimer.Start();
    }

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        lblClock.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy • hh:mm:ss tt");
    }

    private void WireButtonActions()
    {
        btnDeviceSetup.Click += (_, _) => SafeRun("Device Setup", () => OpenForm("DeviceSetupForm"));
        btnFetch.Click += (_, _) => FetchLogs();
        btnEmployees.Click += (_, _) => SafeRun("Manage Employees", () => OpenForm("EmployeeForm"));
        btnEvents.Click += (_, _) => SafeRun("Events / Holidays", () => OpenForm("EventForm"));

        btnGenerate.Click += (_, _) => SafeRun("Generate DTR", GenerateDtr);
        btnViewDtr.Click += (_, _) => SafeRun("View DTR", () => OpenForm("DtrViewerForm"));
        btnRawLogs.Click += (_, _) => SafeRun("Raw Logs", () => OpenForm("RawLogsForm"));
        btnPrintAll.Click += (_, _) => SafeRun("AO Print All", () => OpenForm("PrintAllDtrForm"));

        btnSettings.Click += (_, _) => SafeRun("Settings", OpenSettings);
        btnHealthCheck.Click += (_, _) => SafeRun("Health Check", OpenHealthCheck);
        btnBackup.Click += (_, _) => SafeRun("Backup DB", BackupDatabase);
        btnAuditLogs.Click += (_, _) => SafeRun("Audit Logs", OpenAuditLogs);
        btnMappingCheck.Click += (_, _) => SafeRun("Mapping Check", OpenMappingCheck);
    }

    private void SafeRun(string title, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var realError = ex.InnerException?.Message ?? ex.Message;
            MessageBox.Show(realError, title + " Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log(title + " Error: " + realError);
        }
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

        Log("Database backup completed: " + path);
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
