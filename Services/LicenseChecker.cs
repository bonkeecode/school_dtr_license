using System.Net.Http.Json;
using SchoolDTR.Models;

namespace SchoolDTR.Services;

public class LicenseCheckResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public string DeviceHash { get; set; } = "";
    public string BiometricHash { get; set; } = "";
}

public static class LicenseChecker
{
    public static async Task<LicenseCheckResult> CheckAsync(string biometricSerial)
    {
        string deviceHash = DeviceHelper.GetDeviceHash(AppConfig.SchoolCode);
        string biometricHash = DeviceHelper.GetBiometricHash(AppConfig.SchoolCode, biometricSerial);

        /*
         * ============================================================
         * DEVELOPMENT MODE
         * ============================================================
         * Use this while developing/testing the system without GitHub
         * license activation.
         *
         * CURRENTLY ENABLED:
         * - The system will always pass license checking.
         *
         * BEFORE PRODUCTION:
         * 1. COMMENT OUT the DEVELOPMENT MODE return block below.
         * 2. UNCOMMENT the PRODUCTION GITHUB LICENSE CHECK block.
         * 3. Make sure AppConfig.GitHubLicenseUrl points to your public
         *    raw GitHub licenses.json file.
         * 4. Add this computer's DeviceHash and BiometricHash to GitHub.
         */

        return new LicenseCheckResult
        {
            IsValid = true,
            Message = "Development mode license bypass is enabled.",
            DeviceHash = deviceHash,
            BiometricHash = biometricHash
        };

        /*
         * ============================================================
         * PRODUCTION GITHUB LICENSE CHECK
         * ============================================================
         * To use in production:
         * - UNCOMMENT this whole block.
         * - COMMENT OUT the DEVELOPMENT MODE return block above.
         *
         * This checks:
         * - school_code
         * - device_hash
         * - biometric_hash
         * - status = ACTIVE
         * - expiry date not expired
         */

        // var result = new LicenseCheckResult
        // {
        //     DeviceHash = deviceHash,
        //     BiometricHash = biometricHash
        // };
        //
        // try
        // {
        //     using var client = new HttpClient();
        //     client.Timeout = TimeSpan.FromSeconds(15);
        //
        //     var root = await client.GetFromJsonAsync<LicenseRoot>(AppConfig.GitHubLicenseUrl);
        //
        //     var license = root?.licenses.FirstOrDefault(x =>
        //         string.Equals(x.school_code, AppConfig.SchoolCode, StringComparison.OrdinalIgnoreCase) &&
        //         string.Equals(x.device_hash, deviceHash, StringComparison.OrdinalIgnoreCase) &&
        //         string.Equals(x.biometric_hash, biometricHash, StringComparison.OrdinalIgnoreCase) &&
        //         string.Equals(x.status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
        //         x.expiry.Date >= DateTime.Today);
        //
        //     if (license == null)
        //     {
        //         result.IsValid = false;
        //         result.Message = "License not found, inactive, expired, or not assigned to this device/biometric.";
        //         return result;
        //     }
        //
        //     result.IsValid = true;
        //     result.Message = "Licensed";
        //     return result;
        // }
        // catch (Exception ex)
        // {
        //     result.IsValid = false;
        //     result.Message = "Unable to check license: " + ex.Message;
        //     return result;
        // }
    }
}