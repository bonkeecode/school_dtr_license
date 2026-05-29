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

    private bool compactMode;
    private int sidebarWidth;
    private int pagePadding;
    private int cardGap;
    private int actionButtonWidth;
    private int actionButtonHeight;

    private readonly Color depedBlue = Color.FromArgb(15, 45, 95);
    private readonly Color depedBlueLight = Color.FromArgb(30, 90, 180);
    private readonly Color depedRed = Color.FromArgb(185, 28, 28);
    private readonly Color bgGray = Color.FromArgb(243, 244, 246);
    private readonly Color textDark = Color.FromArgb(31, 41, 55);
    private readonly Color textMuted = Color.FromArgb(75, 85, 99);
    private readonly Label lblSchoolName = new();
    private readonly Label lblSchoolId = new();
    private readonly PictureBox picLogo = new();
    public MainForm()
    {
        Text = "City of Mati National High School (CMNHS) - 305680 DTR System";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1024, 650);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = bgGray;

        ApplyResponsiveMetrics();
        BuildUi();
        WireButtonActions();

        Resize += (_, _) => RebuildForResponsiveSize();

        Log("System ready.");
        ApplyDynamicSchoolSettings();
        ApplyLogoIcon();
    }

    private void ApplyResponsiveMetrics()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1300, 760);
        compactMode = area.Width <= 1366 || area.Height <= 800 || ClientSize.Width <= 1366 || ClientSize.Height <= 800;

        sidebarWidth = compactMode ? 220 : 280;
        pagePadding = compactMode ? 14 : 28;
        cardGap = compactMode ? 8 : 14;
        actionButtonWidth = compactMode ? 142 : 170;
        actionButtonHeight = compactMode ? 48 : 62;
    }

    private void RebuildForResponsiveSize()
    {
        bool wasCompact = compactMode;
        ApplyResponsiveMetrics();

        if (wasCompact != compactMode)
            BuildUi();
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

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, sidebarWidth));
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
            Padding = new Padding(compactMode ? 10 : 18)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 9,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, compactMode ? 78 : 115));
        for (var i = 1; i <= 7; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, compactMode ? 45 : 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var brand = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
            Padding = new Padding(0, compactMode ? 5 : 12, 0, 0)
        };

        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, compactMode ? 44 : 62));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        brand.RowStyles.Add(new RowStyle(SizeType.Absolute, compactMode ? 28 : 32));
        brand.RowStyles.Add(new RowStyle(SizeType.Absolute, compactMode ? 22 : 26));

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        try
        {
            var settings = AppSettingsService.Load();

            if (!string.IsNullOrWhiteSpace(settings.LogoPath) &&
                File.Exists(settings.LogoPath))
            {
                using var fs = new FileStream(settings.LogoPath, FileMode.Open, FileAccess.Read);
                using var img = Image.FromStream(fs);

                logo.Image = new Bitmap(img);
            }
            else
            {
                logo.Image = CreateFallbackLogo(compactMode);
            }
        }
        catch
        {
            logo.Image = CreateFallbackLogo(compactMode);
        }

        var title = new Label
        {
            Text = "School DTR System",
            Font = new Font("Segoe UI", compactMode ? 10 : 13, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.BottomLeft
        };

        var subtitle = new Label
        {
            Text = "Daily Time Record",
            Font = new Font("Segoe UI", compactMode ? 8 : 9, FontStyle.Regular),
            ForeColor = Color.FromArgb(210, 220, 235),
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.TopLeft
        };

        brand.Controls.Add(logo, 0, 0);
        brand.SetRowSpan(logo, 2);
        brand.Controls.Add(title, 1, 0);
        brand.Controls.Add(subtitle, 1, 1);

        layout.Controls.Add(brand, 0, 0);
        layout.Controls.Add(CreateMenuButton("📊  Dashboard", true), 0, 1);
        layout.Controls.Add(CreateMenuButton("👥  Employee Management"), 0, 2);
        layout.Controls.Add(CreateMenuButton("🕒  DTR / Time Logs"), 0, 3);
        layout.Controls.Add(CreateMenuButton("📝  Leave & Accomplishments"), 0, 4);
        layout.Controls.Add(CreateMenuButton("🔐  Biometric Management"), 0, 5);
        layout.Controls.Add(CreateMenuButton("📈  Reports & Analytics"), 0, 6);
        layout.Controls.Add(CreateMenuButton("⚙️  System Settings"), 0, 7);

        sidebar.Controls.Add(layout);
        return sidebar;
    }

    private Button CreateMenuButton(string text, bool active = false)
    {
        var b = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(compactMode ? 8 : 16, 0, 0, 0),
            Font = new Font("Segoe UI", compactMode ? 8.5f : 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = active ? depedBlueLight : depedBlue,
            Cursor = Cursors.Hand,
            AutoEllipsis = true,
            FlatAppearance =
            {
                BorderSize = 0,
                MouseOverBackColor = Color.FromArgb(25, 75, 145),
                MouseDownBackColor = Color.FromArgb(20, 65, 125)
            }
        };

        if (text.Contains("Employee"))
            b.Click += (_, _) => SafeRun("Manage Employees", () => OpenForm("EmployeeForm"));
        else if (text.Contains("DTR"))
            b.Click += (_, _) => SafeRun("View DTR", () => OpenForm("DtrViewerForm"));
        else if (text.Contains("Leave"))
            b.Click += (_, _) => UnderConstruction("Leave & Accomplishments");
        else if (text.Contains("Biometric"))
            b.Click += (_, _) => SafeRun("Device Setup", () => OpenForm("DeviceSetupForm"));
        else if (text.Contains("Reports"))
            b.Click += (_, _) => UnderConstruction("Reports & Analytics");
        else if (text.Contains("Settings"))
            b.Click += (_, _) => SafeRun("Settings", OpenSettings);

        return b;
    }

    private Panel BuildMainDashboard()
    {
        var main = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = bgGray,
            Padding = new Padding(pagePadding),
            AutoScroll = true
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, compactMode ? 54 : 76));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, compactMode ? 86 : 115));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, compactMode ? 188 : 150));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, compactMode ? 225 : 300));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, compactMode ? 105 : 150));

        layout.Controls.Add(BuildTopBar(), 0, 0);
        layout.Controls.Add(BuildWelcomeCard(), 0, 1);
        layout.Controls.Add(BuildStatCards(), 0, 2);
        layout.Controls.Add(BuildActionArea(), 0, 3);
        layout.Controls.Add(BuildLogArea(), 0, 4);

        main.Controls.Add(layout);
        main.Resize += (_, _) => layout.Width = main.ClientSize.Width - main.Padding.Left - main.Padding.Right - SystemInformation.VerticalScrollBarWidth;
        layout.Width = main.ClientSize.Width - main.Padding.Left - main.Padding.Right - SystemInformation.VerticalScrollBarWidth;

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
            Font = new Font("Segoe UI", compactMode ? 19 : 24, FontStyle.Bold),
            ForeColor = textDark,
            AutoSize = true,
            Location = new Point(0, compactMode ? 6 : 8)
        };

        lblClock = new Label
        {
            Font = new Font("Segoe UI", compactMode ? 9 : 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(55, 65, 81),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(compactMode ? 335 : 460, compactMode ? 28 : 36)
        };

        top.Resize += (_, _) => lblClock.Location = new Point(Math.Max(0, top.Width - lblClock.Width), compactMode ? 9 : 14);

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
            Font = new Font("Segoe UI", compactMode ? 13 : 18, FontStyle.Bold),
            ForeColor = textDark,
            AutoSize = false,
            Location = new Point(compactMode ? 14 : 24, compactMode ? 12 : 20),
            Size = new Size(900, compactMode ? 24 : 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoEllipsis = true
        };

        var desc = new Label
        {
            Text = "Manage attendance, biometric logs, CSC Form 48 printing, backups, audit logs, and ZKTeco K14 synchronization.",
            Font = new Font("Segoe UI", compactMode ? 8.5f : 10f, FontStyle.Regular),
            ForeColor = textMuted,
            AutoSize = false,
            Location = new Point(compactMode ? 16 : 26, compactMode ? 40 : 58),
            Size = new Size(950, compactMode ? 22 : 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoEllipsis = true
        };

        card.Resize += (_, _) =>
        {
            title.Width = card.ClientSize.Width - title.Left - 18;
            desc.Width = card.ClientSize.Width - desc.Left - 18;
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
            ColumnCount = compactMode ? 2 : 4,
            RowCount = compactMode ? 2 : 1,
            BackColor = Color.Transparent,
            Padding = new Padding(0, compactMode ? 6 : 12, 0, compactMode ? 6 : 12)
        };

        if (compactMode)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        }
        else
        {
            for (var i = 0; i < 4; i++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        grid.Controls.Add(CreateStatCard("👥", "Employees", "Active Personnel", "Blue"), 0, 0);
        grid.Controls.Add(CreateStatCard("🕒", "Time Logs", "Today's Records", "Blue"), 1, 0);
        grid.Controls.Add(CreateStatCard("⚠️", "Pending Issues", "Logs to Review", "Red"), compactMode ? 0 : 2, compactMode ? 1 : 0);
        grid.Controls.Add(CreateStatCard("🖨️", "Reports", "Ready to Print", "Blue"), compactMode ? 1 : 3, compactMode ? 1 : 0);

        return grid;
    }

    private Panel CreateStatCard(string icon, string title, string subtitle, string accent)
    {
        var card = CreateCard();
        card.Margin = new Padding(0, 0, compactMode ? 8 : 16, compactMode ? 8 : 0);
        card.Padding = new Padding(0);

        var iconLabel = new Label
        {
            Text = icon,
            Font = new Font("Segoe UI Emoji", compactMode ? 19 : 24, FontStyle.Regular),
            ForeColor = Color.FromArgb(17, 24, 39),
            Location = new Point(compactMode ? 12 : 22, compactMode ? 16 : 30),
            Size = new Size(compactMode ? 40 : 52, compactMode ? 40 : 52),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", compactMode ? 10 : 12, FontStyle.Bold),
            ForeColor = textDark,
            Location = new Point(compactMode ? 62 : 92, compactMode ? 18 : 32),
            Size = new Size(210, compactMode ? 22 : 24),
            AutoEllipsis = true
        };

        var subLabel = new Label
        {
            Text = subtitle,
            Font = new Font("Segoe UI", compactMode ? 7.8f : 8f, FontStyle.Regular),
            ForeColor = accent == "Red" ? depedRed : textMuted,
            Location = new Point(compactMode ? 64 : 94, compactMode ? 42 : 58),
            Size = new Size(210, 20),
            AutoEllipsis = true
        };

        card.Resize += (_, _) =>
        {
            titleLabel.Width = Math.Max(50, card.ClientSize.Width - titleLabel.Left - 10);
            subLabel.Width = Math.Max(50, card.ClientSize.Width - subLabel.Left - 10);
        };

        card.Controls.Add(iconLabel);
        card.Controls.Add(titleLabel);
        card.Controls.Add(subLabel);
        return card;
    }

    private Panel BuildActionArea()
    {
        var card = CreateCard();
        card.Padding = new Padding(compactMode ? 14 : 24);

        var title = new Label
        {
            Text = "Quick Actions",
            Font = new Font("Segoe UI", compactMode ? 13 : 16, FontStyle.Bold),
            ForeColor = textDark,
            AutoSize = true,
            Location = new Point(compactMode ? 14 : 24, compactMode ? 12 : 22)
        };

        var subtitle = new Label
        {
            Text = "Frequently used DTR, biometric, reporting, and maintenance tools.",
            Font = new Font("Segoe UI", compactMode ? 8 : 9, FontStyle.Regular),
            ForeColor = textMuted,
            AutoSize = false,
            Location = new Point(compactMode ? 16 : 26, compactMode ? 38 : 50),
            Size = new Size(800, 20),
            AutoEllipsis = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var actions = new FlowLayoutPanel
        {
            Location = new Point(compactMode ? 14 : 24, compactMode ? 65 : 85),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        card.Resize += (_, _) =>
        {
            actions.Size = new Size(card.ClientSize.Width - (compactMode ? 28 : 48), card.ClientSize.Height - actions.Top - 12);
            subtitle.Width = card.ClientSize.Width - subtitle.Left - 16;
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
        card.Padding = new Padding(compactMode ? 10 : 16);

        var title = new Label
        {
            Text = "Activity Log",
            Font = new Font("Segoe UI", compactMode ? 9 : 11, FontStyle.Bold),
            ForeColor = textDark,
            Dock = DockStyle.Top,
            Height = compactMode ? 22 : 26
        };

        txtLog.Dock = DockStyle.Fill;
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.ReadOnly = true;
        txtLog.BorderStyle = BorderStyle.None;
        txtLog.BackColor = Color.White;
        txtLog.ForeColor = Color.FromArgb(55, 65, 81);
        txtLog.Font = new Font("Consolas", compactMode ? 8 : 9);

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
            Margin = new Padding(0, 0, 0, cardGap),
            Padding = new Padding(compactMode ? 10 : 18),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private void StylePrimaryButton(Button button, string icon)
    {
        button.Width = actionButtonWidth;
        button.Height = actionButtonHeight;
        button.Margin = new Padding(0, 0, compactMode ? 8 : 14, compactMode ? 8 : 14);
        button.Text = $"{icon}  {button.Text}";
        button.Font = new Font("Segoe UI", compactMode ? 8.6f : 10f, FontStyle.Bold);
        button.ForeColor = Color.White;
        button.BackColor = depedBlueLight;
        button.FlatStyle = FlatStyle.Flat;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Cursor = Cursors.Hand;
        button.AutoEllipsis = true;
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
        lblClock.Text = DateTime.Now.ToString(compactMode ? "MMM dd, yyyy • hh:mm tt" : "dddd, MMMM dd, yyyy • hh:mm:ss tt");
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

    private void UnderConstruction(string feature)
    {
        MessageBox.Show(
            $"{feature} is still under construction in this beta version.",
            "Beta Feature",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

        Log(feature + " clicked. Feature is still under construction.");
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
    private void ApplyDynamicSchoolSettings()
    {
        var s = AppSettingsService.Load();

        Text = $"{s.SchoolName} - School DTR System";

        lblSchoolName.Text = s.SchoolName;
        lblSchoolId.Text = $"School ID: {s.SchoolId}";

    string logoPath = s.LogoPath;

    if (string.IsNullOrWhiteSpace(logoPath) || !File.Exists(logoPath))
    {
        logoPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SchoolDTR",
            "assets",
            "default_logo.png"
        );
    }

    if (File.Exists(logoPath))
    {
        using var img = Image.FromFile(logoPath);
        picLogo.Image = new Bitmap(img);

        using var bmp = new Bitmap(logoPath);
        Icon = Icon.FromHandle(bmp.GetHicon());
    }
    }
    private void ApplyLogoIcon()
    {
        var settings = AppSettingsService.Load();

        if (string.IsNullOrWhiteSpace(settings.LogoPath))
            return;

        if (!File.Exists(settings.LogoPath))
            return;

        try
        {
            using var bmp = new Bitmap(settings.LogoPath);
            IntPtr hIcon = bmp.GetHicon();
            Icon = Icon.FromHandle(hIcon);
        }
        catch
        {
            // Ignore invalid image/icon errors
        }
    }
    private Image CreateFallbackLogo(bool compactMode)
    {
        int size = compactMode ? 48 : 64;
        var bmp = new Bitmap(size, size);

        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        using var font = new Font("Segoe UI Emoji", compactMode ? 24 : 30);
        using var brush = new SolidBrush(Color.White);

        var text = "🏫";
        var textSize = g.MeasureString(text, font);

        g.DrawString(
            text,
            font,
            brush,
            (size - textSize.Width) / 2,
            (size - textSize.Height) / 2
        );

        return bmp;
    }
}
