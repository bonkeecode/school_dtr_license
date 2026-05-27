using System.Text.Json;
using System.Text.Json.Serialization;
namespace SchoolDTR.Services;

public static class LicenseService
{
    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SchoolDTR",
        "license_cache.json"
    );

    public static async Task<bool> IsLicensedAsync()
    {
        var machineHash = MachineFingerprintService.GetMachineHash();

        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(10);

            var json = await http.GetStringAsync(AppConfig.LicenseJsonUrl);

            Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
            await File.WriteAllTextAsync(CachePath, json);


            // MessageBox.Show(json);


            return IsHashAllowed(json, machineHash);
        }
        catch
        {
            if (File.Exists(CachePath))
            {
                var cachedJson = await File.ReadAllTextAsync(CachePath);
                return IsHashAllowed(cachedJson, machineHash);
            }
        
            return false;
        }
    }

    private static bool IsHashAllowed(string json, string machineHash)
    {
        var data = JsonSerializer.Deserialize<LicenseRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data?.Licenses == null)
            return false;

        var normalizedHash = machineHash.Trim().ToUpperInvariant();
        var normalizedSchool = AppConfig.SchoolCode.Trim();

        var matched = data.Licenses.Any(x =>
            x.IsActive &&
            (x.SchoolId ?? "").Trim() == normalizedSchool &&
            string.Equals(
                (x.MachineHash ?? "").Trim().ToUpperInvariant(),
                normalizedHash,
                StringComparison.OrdinalIgnoreCase
            )
        );



//         MessageBox.Show(
//         $"AppConfig SchoolCode: [{AppConfig.SchoolCode}]\n" +
//         $"JSON SchoolId: [{data.Licenses.FirstOrDefault()?.SchoolId}]\n\n" +
//         $"Machine Hash:\n{normalizedHash}",
//         "License Debug"
// );

        return matched;
    }

    private class LicenseRoot
    {
        public List<LicenseItem> Licenses { get; set; } = new();
    }

private class LicenseItem
{
    [JsonPropertyName("school_id")]
    public string SchoolId { get; set; } = "";

    [JsonPropertyName("machine_hash")]
    public string MachineHash { get; set; } = "";

    [JsonPropertyName("school_name")]
    public string SchoolName { get; set; } = "";

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}
}