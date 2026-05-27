using SchoolDTR.Models;
using SchoolDTR.Services;

namespace SchoolDTR.Forms;

public class DeviceSetupForm : Form
{
    private readonly TextBox txtIp = new();
    private readonly NumericUpDown numPort = new();
    private readonly NumericUpDown numMachine = new();
    private readonly TextBox txtSerial = new();
    private readonly Button btnSave = new();

    public DeviceSetupForm()
    {
        Text = "Biometric Device Setup - ZKTeco K14";
        Width = 460;
        Height = 280;
        StartPosition = FormStartPosition.CenterParent;

        AddLabel("Device Model", 25, 25);
        AddLabel("ZKTeco K14", 170, 25);

        AddLabel("Device IP", 25, 65);
        txtIp.SetBounds(170, 60, 220, 28);

        AddLabel("Port", 25, 105);
        numPort.SetBounds(170, 100, 120, 28);
        numPort.Minimum = 1;
        numPort.Maximum = 65535;
        numPort.Value = 4370;

        AddLabel("Machine No.", 25, 145);
        numMachine.SetBounds(170, 140, 120, 28);
        numMachine.Minimum = 1;
        numMachine.Maximum = 999;
        numMachine.Value = 1;

        AddLabel("Device Serial", 25, 185);
        txtSerial.SetBounds(170, 180, 220, 28);
        txtSerial.PlaceholderText = "Auto-filled after fetch";

        btnSave.Text = "Save";
        btnSave.SetBounds(300, 215, 90, 32);
        btnSave.Click += async (_, _) => await Save();

        Controls.AddRange(new Control[] { txtIp, numPort, numMachine, txtSerial, btnSave });
        Load += async (_, _) => await LoadSettings();
    }

    private void AddLabel(string text, int left, int top)
    {
        Controls.Add(new Label { Text = text, Left = left, Top = top, Width = 135, Height = 25 });
    }

    private async Task LoadSettings()
    {
        var settings = await BiometricSettingsService.GetActiveDeviceAsync();
        txtIp.Text = settings.DeviceIp;
        numPort.Value = settings.DevicePort;
        numMachine.Value = settings.MachineNumber;
        txtSerial.Text = settings.DeviceSerial ?? "";
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(txtIp.Text))
        {
            MessageBox.Show("Please enter the biometric device IP address.");
            return;
        }

        await BiometricSettingsService.SaveAsync(new BiometricDeviceSettings
        {
            SchoolId = AppConfig.SchoolCode,
            DeviceModel = "ZKTeco K14",
            DeviceIp = txtIp.Text.Trim(),
            DevicePort = (int)numPort.Value,
            MachineNumber = (int)numMachine.Value,
            DeviceSerial = string.IsNullOrWhiteSpace(txtSerial.Text) ? null : txtSerial.Text.Trim()
        });

        MessageBox.Show("Biometric device settings saved.");
        DialogResult = DialogResult.OK;
        Close();
    }
}
