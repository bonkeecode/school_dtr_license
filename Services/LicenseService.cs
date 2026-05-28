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
            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            var json = await http.GetStringAsync(AppConfig.LicenseJsonUrl);

            // Save fresh copy to cache
            Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
            await File.WriteAllTextAsync(CachePath, json);

            return IsHashAllowed(json, machineHash);
        }
        catch
        {
            // Internet unavailable → use cached license only
            if (!File.Exists(CachePath))
                return false;

            try
            {
                var cachedJson = await File.ReadAllTextAsync(CachePath);
                return IsHashAllowed(cachedJson, machineHash);
            }
            catch
            {
                return false;
            }
        }
    }

    private static bool IsHashAllowed(string json, string machineHash)
    {
        try
        {
            var data = JsonSerializer.Deserialize<LicenseRoot>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (data?.Licenses == null || data.Licenses.Count == 0)
                return false;

            var normalizedHash = machineHash
                .Trim()
                .ToUpperInvariant();

            var normalizedSchool = AppConfig.SchoolCode
                .Trim();

            var license = data.Licenses.FirstOrDefault(x =>
                x.IsActive &&
                string.Equals(
                    (x.SchoolId ?? "").Trim(),
                    normalizedSchool,
                    StringComparison.OrdinalIgnoreCase
                ) &&
                string.Equals(
                    (x.MachineHash ?? "")
                        .Trim()
                        .ToUpperInvariant(),
                    normalizedHash,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if (license == null)
                return false;

            // Expiration date is REQUIRED
            if (license.ExpiresOn == null)
                return false;

            var today = DateTime.Today;
            var expiryDate = license.ExpiresOn.Value.Date;

            // License expired
            if (today > expiryDate)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private class LicenseRoot
    {
        [JsonPropertyName("licenses")]
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

        // Supports both "is_active" and "active"
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("active")]
        public bool Active
        {
            set => IsActive = value;
        }

        // REQUIRED
        [JsonPropertyName("expires_on")]
        public DateTime? ExpiresOn { get; set; }
    }
}