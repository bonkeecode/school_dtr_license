using System.Text.Json;

namespace SchoolDTR.Services;

public class AppSettings
{
    public string SchoolId { get; set; } = "";
    public string SchoolName { get; set; } = "";
    public string DeviceIp { get; set; } = "192.168.1.201";
    public int DevicePort { get; set; } = 4370;

    public string DbHost { get; set; } = "localhost";
    public string DbName { get; set; } = "school_dtr";
    public string DbUser { get; set; } = "root";
    public string DbPassword { get; set; } = "";
    public string DeviceModel { get; set; } = "ZKTeco Compatible";
    public int MachineNumber { get; set; } = 1;
}

public static class AppSettingsService
{
    private static readonly string FilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.local.json");

    public static AppSettings Load()
    {
        if (!File.Exists(FilePath))
            return new AppSettings();

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(FilePath, json);
    }
}