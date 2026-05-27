using System.Data;
using MySqlConnector;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class AuditLogsForm : Form
{
    private readonly DateTimePicker dtFrom = new();
    private readonly DateTimePicker dtTo = new();
    private readonly ComboBox cmbAction = new();
    private readonly DataGridView grid = new();

    public AuditLogsForm()
    {
        Text = "Audit Logs";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
        AuditLogService.EnsureTable();
        LoadActions();
        LoadLogs();
    }

    private void BuildUi()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(15)
        };

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        dtFrom.Width = 120;
        dtFrom.Format = DateTimePickerFormat.Short;
        dtFrom.Value = DateTime.Today.AddDays(-30);

        dtTo.Width = 120;
        dtTo.Format = DateTimePickerFormat.Short;
        dtTo.Value = DateTime.Today;

        cmbAction.Width = 180;
        cmbAction.DropDownStyle = ComboBoxStyle.DropDownList;

        var btnLoad = new Button
        {
            Text = "Load",
            Width = 100,
            Height = 32
        };

        btnLoad.Click += (_, _) => LoadLogs();

        top.Controls.Add(new Label { Text = "From:", AutoSize = true, Padding = new Padding(0, 8, 5, 0) });
        top.Controls.Add(dtFrom);

        top.Controls.Add(new Label { Text = "To:", AutoSize = true, Padding = new Padding(15, 8, 5, 0) });
        top.Controls.Add(dtTo);

        top.Controls.Add(new Label { Text = "Action:", AutoSize = true, Padding = new Padding(15, 8, 5, 0) });
        top.Controls.Add(cmbAction);
        top.Controls.Add(btnLoad);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        main.Controls.Add(top, 0, 0);
        main.Controls.Add(grid, 0, 1);

        Controls.Add(main);
    }

    private void LoadActions()
    {
        cmbAction.Items.Clear();
        cmbAction.Items.Add("ALL");

        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT DISTINCT action
            FROM audit_logs
            ORDER BY action;
        ", conn);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
            cmbAction.Items.Add(reader.GetString(0));

        cmbAction.SelectedIndex = 0;
    }

    private void LoadLogs()
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT
                created_at AS `Date/Time`,
                action AS `Action`,
                description AS `Description`,
                performed_by AS `Performed By`,
                computer_name AS `Computer`
            FROM audit_logs
            WHERE DATE(created_at) BETWEEN @from AND @to
              AND (
                    @action = 'ALL'
                    OR action = @action
                  )
            ORDER BY created_at DESC;
        ", conn);

        cmd.Parameters.AddWithValue("@from", dtFrom.Value.Date);
        cmd.Parameters.AddWithValue("@to", dtTo.Value.Date);
        cmd.Parameters.AddWithValue("@action", cmbAction.Text);

        using var da = new MySqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);

        grid.DataSource = dt;
    }
}