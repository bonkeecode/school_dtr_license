using System.Security.Cryptography;

namespace SchoolDTR.Services;

public static class AppIntegrityService
{
    private static readonly string[] RequiredFiles =
    {
        "SchoolDTR.exe",
        "SchoolDTR.dll"
    };

    public static bool IsAppIntact()
    {
        try
        {
            string baseDir = AppContext.BaseDirectory;

            foreach (string fileName in RequiredFiles)
            {
                string path = Path.Combine(baseDir, fileName);

                if (!File.Exists(path))
                    return false;

                using var sha = SHA256.Create();
                using var stream = File.OpenRead(path);

                string hash = Convert.ToHexString(sha.ComputeHash(stream));

                if (!ExpectedHashes.TryGetValue(fileName, out string? expectedHash))
                    return false;

                if (!string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static readonly Dictionary<string, string> ExpectedHashes = new()
    {
        // Replace after publish
        ["SchoolDTR.exe"] = "PASTE_EXE_SHA256_HERE",
        ["SchoolDTR.dll"] = "PASTE_DLL_SHA256_HERE"
    };
}