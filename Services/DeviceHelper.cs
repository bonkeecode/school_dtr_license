using Microsoft.Win32;

namespace SchoolDTR.Services;

public static class DeviceHelper
{
    public static string GetMachineGuid()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        return key?.GetValue("MachineGuid")?.ToString() ?? "";
    }

    public static string GetDeviceHash(string schoolCode)
    {
        string raw = $"{schoolCode}|{GetMachineGuid()}";
        return HashHelper.Sha256(raw);
    }

    public static string GetBiometricHash(string schoolCode, string biometricSerial)
    {
        string raw = $"{schoolCode}|{biometricSerial}";
        return HashHelper.Sha256(raw);
    }
}
