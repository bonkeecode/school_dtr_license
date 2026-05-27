using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class StartupForm : Form
{
    private readonly Label lblInfo = new();
    private readonly Button btnCheck = new();
    private readonly TextBox txtDeviceHash = new();
    private readonly TextBox txtBiometricHash = new();

    public StartupForm()
    {
        Text = "School DTR - License Check";
        Width = 760;
        Height = 360;
        StartPosition = FormStartPosition.CenterScreen;

        lblInfo.Text = $"School: {AppConfig.SchoolName}\nSchool ID: {AppConfig.SchoolCode}";
        lblInfo.Left = 20;
        lblInfo.Top = 20;
        lblInfo.Width = 700;
        lblInfo.Height = 50;

        btnCheck.Text = "Check License";
        btnCheck.Left = 20;
        btnCheck.Top = 85;
        btnCheck.Width = 160;
        btnCheck.Click += async (_, _) => await CheckLicense();

        txtDeviceHash.Left = 20;
        txtDeviceHash.Top = 135;
        txtDeviceHash.Width = 700;
        txtDeviceHash.ReadOnly = true;

        txtBiometricHash.Left = 20;
        txtBiometricHash.Top = 180;
        txtBiometricHash.Width = 700;
        txtBiometricHash.ReadOnly = true;

        Controls.Add(lblInfo);
        Controls.Add(btnCheck);
        Controls.Add(txtDeviceHash);
        Controls.Add(txtBiometricHash);

        Load += async (_, _) => await CheckLicense();
    }

    private async Task CheckLicense()
    {
        btnCheck.Enabled = false;

        var result = await LicenseChecker.CheckAsync(AppConfig.DefaultBiometricSerial);

        txtDeviceHash.Text = "device_hash: " + result.DeviceHash;
        txtBiometricHash.Text = "biometric_hash: " + result.BiometricHash;

        if (!result.IsValid)
        {
            MessageBox.Show(
                result.Message + "\n\nCopy the hashes shown and add them to your GitHub licenses.json.",
                "License not valid",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            btnCheck.Enabled = true;
            return;
        }

        Hide();
        new MainForm().ShowDialog();
        Close();
    }
}
