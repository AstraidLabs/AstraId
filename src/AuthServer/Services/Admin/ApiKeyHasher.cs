using System.Security.Cryptography;
using System.Text;

namespace AuthServer.Services.Admin;

public static class ApiKeyHasher
{
    public static string HashApiKey(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyApiKey(string apiKey, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var hash = HashApiKey(apiKey);
        var hashBytes = Convert.FromBase64String(hash);
        var storedBytes = Convert.FromBase64String(storedHash);
        return CryptographicOperations.FixedTimeEquals(hashBytes, storedBytes);
    }

    public static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
