using SchoolDTR.Services;

namespace SchoolDTR.Models;

public sealed class BiometricDeviceSettings
{
    public string SchoolId { get; set; }
    public string DeviceModel { get; set; }
    public string DeviceIp { get; set; }
    public int DevicePort { get; set; }
    public int MachineNumber { get; set; }
    public string? DeviceSerial { get; set; }

    public BiometricDeviceSettings()
    {
        var settings = AppSettingsService.Load();

        SchoolId = settings.SchoolId;
        DeviceModel = settings.DeviceModel;
        DeviceIp = settings.DeviceIp;
        DevicePort = settings.DevicePort;
        MachineNumber = settings.MachineNumber;
    }
}

public sealed class BiometricFetchResult
{
    public bool Success { get; set; }
    public int TotalLogs { get; set; }
    public int InsertedLogs { get; set; }
    public int DuplicateLogs { get; set; }
    public string Message { get; set; } = "";
    public string RawOutput { get; set; } = "";
}