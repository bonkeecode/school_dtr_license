using SchoolDTR;

namespace SchoolDTR.Models;

public sealed class BiometricDeviceSettings
{
    public string SchoolId { get; set; } = AppConfig.SchoolCode;
    public string DeviceModel { get; set; } = "ZKTeco K14";
    public string DeviceIp { get; set; } = "192.168.1.201";
    public int DevicePort { get; set; } = 4370;
    public int MachineNumber { get; set; } = 1;
    public string? DeviceSerial { get; set; }
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
