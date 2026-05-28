using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchoolDTR.Services;

public static class LicenseService
{
    private static readonly string CacheFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SchoolDTR"
    );

    private static readonly string CachePath = Path.Combine(
        CacheFolder,
        "license_cache_v2.json"
    );

    public static async Task<bool> IsLicensedAsync()
    {
        string machineHash = MachineFingerprintService.GetMachineHash();

        // Important: remove old cache before checking online license.
        ForceClearLicenseCache();

        string json;

        try
        {
            json = await FetchOnlineLicenseJsonAsync();
        }
        catch
        {
            // No offline authorization.
            // If GitHub cannot be reached, deny access.
            ForceClearLicenseCache();
            return false;
        }

        bool allowed = IsHashAllowed(json, machineHash);

        if (!allowed)
        {
            ForceClearLicenseCache();
            return false;
        }

        // Cache is only for reference/debug.
        // The app must never authorize from this file.
        SaveCacheForReferenceOnly(json);

        return true;
    }

    private static async Task<string> FetchOnlineLicenseJsonAsync()
    {
        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MaxAge = TimeSpan.Zero
        };

        http.DefaultRequestHeaders.Pragma.ParseAdd("no-cache");
        http.DefaultRequestHeaders.UserAgent.ParseAdd("SchoolDTR-LicenseChecker/1.0");

        string url = AppConfig.LicenseJsonUrl.Trim();

        url += url.Contains("?") ? "&" : "?";
        url += "nocache=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return await http.GetStringAsync(url);
    }

    private static void SaveCacheForReferenceOnly(string json)
    {
        try
        {
            Directory.CreateDirectory(CacheFolder);
            File.WriteAllText(CachePath, json);
        }
        catch
        {
            // Cache is optional. Do not allow or deny based on this.
        }
    }

    private static void ForceClearLicenseCache()
    {
        try
        {
            if (!Directory.Exists(CacheFolder))
                return;

            foreach (string file in Directory.GetFiles(CacheFolder, "*.json"))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch
                {
                    // Ignore locked files.
                    // They are no longer trusted anyway.
                }
            }
        }
        catch
        {
            // Do not crash the app because of cleanup failure.
        }
    }

    private static bool IsHashAllowed(string json, string machineHash)
    {
        try
        {
            var data = JsonSerializer.Deserialize<LicenseRoot>(json, JsonOptions);

            if (data?.Licenses == null || data.Licenses.Count == 0)
                return false;

            string normalizedHash = machineHash.Trim().ToUpperInvariant();
            string normalizedSchool = AppConfig.SchoolCode.Trim();

            var license = data.Licenses.FirstOrDefault(x =>
                x.IsActive &&
                string.Equals(
                    (x.SchoolId ?? "").Trim(),
                    normalizedSchool,
                    StringComparison.OrdinalIgnoreCase
                ) &&
                string.Equals(
                    (x.MachineHash ?? "").Trim().ToUpperInvariant(),
                    normalizedHash,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if (license == null)
                return false;

            if (!license.ExpiresOn.HasValue)
                return false;

            if (DateTime.Today > license.ExpiresOn.Value.Date)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

        [JsonPropertyName("expires_on")]
        public DateTime? ExpiresOn { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("active")]
        public bool Active
        {
            set => IsActive = value;
        }
    }
}