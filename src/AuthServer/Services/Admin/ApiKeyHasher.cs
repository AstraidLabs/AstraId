using System.Security.Cryptography;
using System.Text;

namespace AuthServer.Services.Admin;

/// <summary>
/// Provides api key hasher functionality.
/// </summary>
public static class ApiKeyHasher
{
    private const string VersionPrefix = "v2";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string HashApiKey(string apiKey)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(apiKey, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{VersionPrefix}:pbkdf2-sha256:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyApiKey(string apiKey, string? storedHash, out string? upgradedHash)
    {
        upgradedHash = null;
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        if (storedHash.StartsWith($"{VersionPrefix}:", StringComparison.Ordinal))
        {
            return VerifyV2(apiKey, storedHash);
        }

        var legacyMatch = VerifyLegacySha256(apiKey, storedHash);
        if (legacyMatch)
        {
            upgradedHash = HashApiKey(apiKey);
        }

        return legacyMatch;
    }

    public static bool VerifyApiKey(string apiKey, string? storedHash)
    {
        return VerifyApiKey(apiKey, storedHash, out _);
    }

    public static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyV2(string apiKey, string storedHash)
    {
        var parts = storedHash.Split(':', StringSplitOptions.None);
        if (parts.Length != 5 || !int.TryParse(parts[2], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;

        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(apiKey, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool VerifyLegacySha256(string apiKey, string storedHash)
    {
        try
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
            var storedBytes = Convert.FromBase64String(storedHash);
            return CryptographicOperations.FixedTimeEquals(hash, storedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
