namespace SchoolDTR.Forms;

public class FetchLogsDateRangeForm : Form
{
    public DateTime DateFrom => dtFrom.Value.Date;
    public DateTime DateTo => dtTo.Value.Date;

    private readonly DateTimePicker dtFrom = new();
    private readonly DateTimePicker dtTo = new();
    private readonly Button btnOk = new();
    private readonly Button btnCancel = new();

    public FetchLogsDateRangeForm()
    {
        Text = "Fetch Logs - Inclusive Dates";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 360;
        Height = 210;

        var lblFrom = new Label { Text = "Date From:", Left = 25, Top = 25, Width = 100 };
        dtFrom.Left = 130;
        dtFrom.Top = 20;
        dtFrom.Width = 170;
        dtFrom.Format = DateTimePickerFormat.Short;

        var lblTo = new Label { Text = "Date To:", Left = 25, Top = 65, Width = 100 };
        dtTo.Left = 130;
        dtTo.Top = 60;
        dtTo.Width = 170;
        dtTo.Format = DateTimePickerFormat.Short;

        btnOk.Text = "Fetch";
        btnOk.Left = 130;
        btnOk.Top = 110;
        btnOk.Width = 80;
        btnOk.DialogResult = DialogResult.OK;

        btnCancel.Text = "Cancel";
        btnCancel.Left = 220;
        btnCancel.Top = 110;
        btnCancel.Width = 80;
        btnCancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            lblFrom, dtFrom,
            lblTo, dtTo,
            btnOk, btnCancel
        });

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && DateFrom > DateTo)
        {
            MessageBox.Show(
                "Date From cannot be later than Date To.",
                "Invalid Date Range",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            e.Cancel = true;
        }

        base.OnFormClosing(e);
    }
}