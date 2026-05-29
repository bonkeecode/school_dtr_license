using System.Text.Json;

namespace SchoolDTR.Services;

public class AppSettings
{
    // School
    public string SchoolId { get; set; } = "305680";
    public string SchoolName { get; set; } =
        "City of Mati National High School";

    // Logo
    public string LogoPath { get; set; } = "";

    // Biometric Device
    public string DeviceIp { get; set; } = "192.168.1.201";
    public int DevicePort { get; set; } = 4370;
    public string DeviceModel { get; set; } = "ZKTeco Compatible";
    public int MachineNumber { get; set; } = 1;

    // Database
    public string DbHost { get; set; } = "localhost";
    public string DbName { get; set; } = "school_dtr_305680";
    public string DbUser { get; set; } = "root";
    public string DbPassword { get; set; } = "";
}

public static class AppSettingsService
{
    private static readonly string AppDataFolder =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SchoolDTR"
        );

    private static readonly string FilePath =
        Path.Combine(AppDataFolder, "appsettings.local.json");

    private static readonly string AssetsFolder =
        Path.Combine(AppDataFolder, "assets");

    public static AppSettings Load()
    {
        try
        {
            EnsureFolders();

            if (!File.Exists(FilePath))
                return new AppSettings();

            var json = File.ReadAllText(FilePath);

            return JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        EnsureFolders();

        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(FilePath, json);
    }

    public static string GetAssetsFolder()
    {
        EnsureFolders();
        return AssetsFolder;
    }

    public static string GetDefaultLogoPath()
    {
        EnsureFolders();

        return Path.Combine(
            AssetsFolder,
            "school_logo.png"
        );
    }

    private static void EnsureFolders()
    {
        if (!Directory.Exists(AppDataFolder))
            Directory.CreateDirectory(AppDataFolder);

        if (!Directory.Exists(AssetsFolder))
            Directory.CreateDirectory(AssetsFolder);
    }
}