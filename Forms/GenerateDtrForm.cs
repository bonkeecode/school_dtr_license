using SchoolDTR.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SchoolDTR.Forms;

public class GenerateDtrForm : Form
{
    private readonly DateTimePicker dtMonth = new();
    private readonly Button btnGenerate = new();

    public GenerateDtrForm()
    {
        Text = "Generate DTR";
        Width = 420;
        Height = 180;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
    }

    private void BuildUi()
    {
        Label lblMonth = new Label
        {
            Text = "Select Month",
            Left = 30,
            Top = 35,
            Width = 120
        };

        dtMonth.Left = 160;
        dtMonth.Top = 30;
        dtMonth.Width = 180;
        dtMonth.Format = DateTimePickerFormat.Custom;
        dtMonth.CustomFormat = "MMMM yyyy";
        dtMonth.ShowUpDown = true;

        btnGenerate.Text = "Generate";
        btnGenerate.Left = 160;
        btnGenerate.Top = 80;
        btnGenerate.Width = 120;
        btnGenerate.Height = 35;
        btnGenerate.Click += async (_, _) => await GenerateAsync();

        Controls.AddRange(new Control[]
        {
            lblMonth,
            dtMonth,
            btnGenerate
        });
    }

    private async Task GenerateAsync()
    {
        int year = dtMonth.Value.Year;
        int month = dtMonth.Value.Month;

        DateTime startDate = new DateTime(year, month, 1);

        btnGenerate.Enabled = false;

        try
        {
            await Task.Run(() => DtrGenerator.GenerateMonth(year, month));

            MessageBox.Show(
                $"DTR generated for {startDate:MMMM yyyy}.",
                "Generate DTR",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Failed to generate DTR: " + ex.Message,
                "Generate DTR Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnGenerate.Enabled = true;
        }
    }
}