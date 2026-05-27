using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace SchoolDTR.Services;

public static class MachineFingerprintService
{
    public static string GetMachineHash()
    {
        var raw = string.Join("|",
            GetWmiValue("Win32_BIOS", "SerialNumber"),
            GetWmiValue("Win32_BaseBoard", "SerialNumber"),
            GetWmiValue("Win32_ComputerSystemProduct", "UUID"),
            GetWmiValue("Win32_DiskDrive", "SerialNumber")
        );

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

        return Convert.ToHexString(bytes);
    }

    private static string GetWmiValue(string className, string property)
    {
        try
        {
            using var searcher =
                new ManagementObjectSearcher($"SELECT {property} FROM {className}");

            foreach (ManagementObject obj in searcher.Get())
            {
                var value = obj[property]?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
        }
        catch
        {
        }

        return "";
    }
}