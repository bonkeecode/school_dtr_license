using SchoolDTR.Forms;
using SchoolDTR.Services;

namespace SchoolDTR;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            if (!LicenseService.IsLicensed())
            {
                ShowMachineHashWindow();
                return;
            }

            Application.Run(new StartupForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private static void ShowMachineHashWindow()
    {
        var hash = MachineFingerprintService.GetMachineHash();

        using var form = new Form
        {
            Text = "Unlicensed Laptop",
            Width = 650,
            Height = 250,
            StartPosition = FormStartPosition.CenterScreen
        };

        var label = new Label
        {
            Text = "This laptop is not licensed.\n\nCopy the machine hash below and add it to the system_license table:",
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(15),
            Font = new Font("Segoe UI", 10)
        };

        var txtHash = new TextBox
        {
            Text = hash,
            ReadOnly = true,
            Dock = DockStyle.Top,
            Margin = new Padding(15),
            Font = new Font("Consolas", 10)
        };

        var btnCopy = new Button
        {
            Text = "Copy Hash",
            Width = 120,
            Height = 35,
            Left = 15,
            Top = 140
        };

        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(hash);
            MessageBox.Show("Machine hash copied.", "Copied");
        };

        form.Controls.Add(btnCopy);
        form.Controls.Add(txtHash);
        form.Controls.Add(label);

        form.ShowDialog();
    }
}