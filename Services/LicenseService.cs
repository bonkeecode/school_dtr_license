using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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
        "license_cache_v2.bin"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<bool> IsLicensedAsync()
    {
        string machineHash = MachineFingerprintService.GetMachineHash();

        string json;

        try
        {
            json = await FetchOnlineLicenseJsonAsync();
        }
        catch
        {
            var cache = ReadEncryptedCacheEnvelope();

            if (cache == null)
            {
                DeleteCache();
                return false;
            }

            if (!IsCacheStillSafe(cache))
            {
                DeleteCache();
                return false;
            }

            if (!IsHashAllowed(cache.Json, machineHash))
            {
                DeleteCache();
                return false;
            }

            UpdateCacheLastRun(cache);
            return true;
        }

        bool allowed = IsHashAllowed(json, machineHash);

        if (!allowed)
        {
            DeleteCache();
            return false;
        }

        SaveEncryptedCache(json);
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

    private static bool IsHashAllowed(string json, string machineHash)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("payload", out var payload))
                return false;

            if (!doc.RootElement.TryGetProperty("signature", out var signatureElement))
                return false;

            string signature = signatureElement.GetString() ?? "";

            if (!LicenseSignatureService.VerifyPayload(payload, signature))
                return false;

            if (!payload.TryGetProperty("licenses", out var licensesElement))
                return false;

            var licenses = JsonSerializer.Deserialize<List<LicenseItem>>(
                licensesElement.GetRawText(),
                JsonOptions
            );

            if (licenses == null || licenses.Count == 0)
                return false;

            string normalizedHash = machineHash.Trim().ToUpperInvariant();
            string normalizedSchool = AppConfig.SchoolCode.Trim();

            var license = licenses.FirstOrDefault(x =>
                x.IsActive &&
                string.Equals((x.SchoolId ?? "").Trim(), normalizedSchool, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((x.MachineHash ?? "").Trim().ToUpperInvariant(), normalizedHash, StringComparison.OrdinalIgnoreCase)
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

    private static void SaveEncryptedCache(string json)
    {
        try
        {
            Directory.CreateDirectory(CacheFolder);

            var envelope = new LicenseCacheEnvelope
            {
                Json = json,
                LastOnlineCheckUtc = DateTime.UtcNow,
                LastRunUtc = DateTime.UtcNow
            };

            string envelopeJson = JsonSerializer.Serialize(envelope, JsonOptions);
            byte[] plainBytes = Encoding.UTF8.GetBytes(envelopeJson);

            byte[] encrypted = ProtectedData.Protect(
                plainBytes,
                null,
                DataProtectionScope.LocalMachine
            );

            File.WriteAllBytes(CachePath, encrypted);
        }
        catch
        {
        }
    }

    private static void DeleteCache()
    {
        try
        {
            if (File.Exists(CachePath))
                File.Delete(CachePath);

            foreach (string file in Directory.GetFiles(CacheFolder, "license_cache*.json"))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch
        {
            // ignore
        }
    }
    private class LicenseCacheEnvelope
    {
        public string Json { get; set; } = "";
        public DateTime LastOnlineCheckUtc { get; set; }
        public DateTime LastRunUtc { get; set; }
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
    private static LicenseCacheEnvelope? ReadEncryptedCacheEnvelope()
    {
        try
        {
            if (!File.Exists(CachePath))
                return null;

            byte[] encrypted = File.ReadAllBytes(CachePath);

            byte[] decrypted = ProtectedData.Unprotect(
                encrypted,
                null,
                DataProtectionScope.LocalMachine
            );

            string envelopeJson = Encoding.UTF8.GetString(decrypted);

            return JsonSerializer.Deserialize<LicenseCacheEnvelope>(
                envelopeJson,
                JsonOptions
            );
        }
        catch
        {
            return null;
        }
    }
    private static bool IsCacheStillSafe(LicenseCacheEnvelope cache)
    {
        var now = DateTime.UtcNow;

        // Clock rollback detection
        if (now < cache.LastRunUtc.AddMinutes(-5))
            return false;

        // Offline grace period: 7 days only
        if (now > cache.LastOnlineCheckUtc.AddDays(7))
            return false;

        return true;
    }
    private static void UpdateCacheLastRun(LicenseCacheEnvelope cache)
    {
        try
        {
            cache.LastRunUtc = DateTime.UtcNow;

            string envelopeJson = JsonSerializer.Serialize(cache, JsonOptions);
            byte[] plainBytes = Encoding.UTF8.GetBytes(envelopeJson);

            byte[] encrypted = ProtectedData.Protect(
                plainBytes,
                null,
                DataProtectionScope.LocalMachine
            );

            File.WriteAllBytes(CachePath, encrypted);
        }
        catch
        {
        }
    }
}