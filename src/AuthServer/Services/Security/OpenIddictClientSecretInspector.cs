using System.Data;
using System.Text.RegularExpressions;
using AuthServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Security;

public interface IOpenIddictClientSecretInspector
{
    Task<(int total, int looksHashed, int looksPlaintext)> InspectAsync(CancellationToken cancellationToken);
}

public sealed class OpenIddictClientSecretInspector : IOpenIddictClientSecretInspector
{
    private static readonly Regex ModularCryptPrefixPattern = new(
        @"^\$(2[aby]|argon2(id|i|d)?|scrypt|pbkdf2)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly ApplicationDbContext _dbContext;

    public OpenIddictClientSecretInspector(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(int total, int looksHashed, int looksPlaintext)> InspectAsync(CancellationToken cancellationToken)
    {
        await using var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"ClientSecret\" FROM \"OpenIddictApplications\" WHERE \"ClientType\" = 'confidential' AND \"ClientSecret\" IS NOT NULL";

        var total = 0;
        var looksHashed = 0;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            total++;
            var secret = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            if (LooksHashed(secret))
            {
                looksHashed++;
            }
        }

        return (total, looksHashed, total - looksHashed);
    }

    private static bool LooksHashed(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.StartsWith("AQAAAA", StringComparison.Ordinal)
               || value.StartsWith("AAEAAAD", StringComparison.Ordinal)
               || value.StartsWith("sha256$", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("pbkdf2$", StringComparison.OrdinalIgnoreCase)
               || ModularCryptPrefixPattern.IsMatch(value);
    }
}
