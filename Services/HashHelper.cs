using System.Security.Cryptography;
using System.Text;

namespace SchoolDTR.Services;

public static class HashHelper
{
    public static string Sha256(string input)
    {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? ""));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
