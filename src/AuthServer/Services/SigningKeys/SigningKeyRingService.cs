using System.Security.Cryptography;
using System.Text.Json;
using AuthServer.Data;
using AuthServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Services.SigningKeys;

public sealed class SigningKeyRingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISigningKeyProtector _protector;
    private readonly IOptionsMonitor<AuthServerSigningKeyOptions> _options;
    private readonly ILogger<SigningKeyRingService> _logger;

    public SigningKeyRingService(
        ApplicationDbContext dbContext,
        ISigningKeyProtector protector,
        IOptionsMonitor<AuthServerSigningKeyOptions> options,
        ILogger<SigningKeyRingService> logger)
    {
        _dbContext = dbContext;
        _protector = protector;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SigningKeyRingEntry>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.SigningKeyRingEntries
            .Where(entry => entry.Status == SigningKeyStatus.Active || entry.Status == SigningKeyStatus.Previous)
            .OrderBy(entry => entry.Status)
            .ThenByDescending(entry => entry.ActivatedUtc ?? entry.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SigningKeyRingEntry>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.SigningKeyRingEntries
            .OrderBy(entry => entry.Status)
            .ThenByDescending(entry => entry.ActivatedUtc ?? entry.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<SigningKeyRingEntry?> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.SigningKeyRingEntries
            .Where(entry => entry.Status == SigningKeyStatus.Active)
            .OrderByDescending(entry => entry.ActivatedUtc ?? entry.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SigningKeyRingEntry> EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        var active = await GetActiveAsync(cancellationToken);
        if (active is not null)
        {
            return active;
        }

        var created = CreateKeyEntry();
        created.Status = SigningKeyStatus.Active;
        created.ActivatedUtc = created.CreatedUtc;

        _dbContext.SigningKeyRingEntries.Add(created);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized signing key ring with new active key {Kid}.", created.Kid);
        return created;
    }

    public async Task<SigningKeyRotationResult> RotateNowAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var active = await GetActiveAsync(cancellationToken);
        var previous = await _dbContext.SigningKeyRingEntries
            .Where(entry => entry.Status == SigningKeyStatus.Previous)
            .OrderByDescending(entry => entry.ActivatedUtc ?? entry.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (active is not null)
        {
            active.Status = SigningKeyStatus.Previous;
            active.RetiredUtc = null;
            active.RevokedUtc = null;
            active.RetireAfterUtc = GetRetireAfterUtc(now);
            _dbContext.SigningKeyRingEntries.Update(active);
        }

        if (previous is not null)
        {
            previous.Status = SigningKeyStatus.Retired;
            previous.RetiredUtc = now;
            previous.RetireAfterUtc = null;
            _dbContext.SigningKeyRingEntries.Update(previous);
        }

        var created = CreateKeyEntry();
        created.Status = SigningKeyStatus.Active;
        created.ActivatedUtc = now;

        _dbContext.SigningKeyRingEntries.Add(created);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SigningKeyRotationResult(created, active);
    }

    public async Task<SigningKeyRingEntry?> RetireAsync(string kid, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.SigningKeyRingEntries
            .FirstOrDefaultAsync(current => current.Kid == kid, cancellationToken);

        if (entry is null)
        {
            return null;
        }

        if (entry.Status == SigningKeyStatus.Active)
        {
            throw new InvalidOperationException("Active signing keys cannot be retired directly.");
        }

        if (entry.Status == SigningKeyStatus.Previous)
        {
            entry.Status = SigningKeyStatus.Retired;
            entry.RetiredUtc = DateTime.UtcNow;
            entry.RetireAfterUtc = null;
            _dbContext.SigningKeyRingEntries.Update(entry);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return entry;
    }

    public async Task<SigningKeyRevokeResult> RevokeAsync(string kid, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.SigningKeyRingEntries
            .FirstOrDefaultAsync(current => current.Kid == kid, cancellationToken);

        if (entry is null)
        {
            return SigningKeyRevokeResult.NotFound;
        }

        var wasActive = entry.Status == SigningKeyStatus.Active;
        if (wasActive)
        {
            var created = CreateKeyEntry();
            created.Status = SigningKeyStatus.Active;
            created.ActivatedUtc = DateTime.UtcNow;
            _dbContext.SigningKeyRingEntries.Add(created);
        }

        var revokedAt = DateTime.UtcNow;
        entry.Status = SigningKeyStatus.Revoked;
        entry.RevokedUtc = revokedAt;
        entry.RetiredUtc = revokedAt;
        entry.RetireAfterUtc = null;
        _dbContext.SigningKeyRingEntries.Update(entry);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return wasActive ? SigningKeyRevokeResult.ReplacedActive : SigningKeyRevokeResult.Revoked;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var retentionDays = Math.Max(0, _options.CurrentValue.PreviousKeyRetentionDays);
        var now = DateTime.UtcNow;
        var retireAfter = await _dbContext.SigningKeyRingEntries
            .Where(entry => entry.Status == SigningKeyStatus.Previous && entry.RetireAfterUtc != null && entry.RetireAfterUtc <= now)
            .ToListAsync(cancellationToken);

        if (retireAfter.Count > 0)
        {
            foreach (var entry in retireAfter)
            {
                entry.Status = SigningKeyStatus.Retired;
                entry.RetiredUtc = now;
                entry.RetireAfterUtc = null;
            }

            _dbContext.SigningKeyRingEntries.UpdateRange(retireAfter);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var cutoff = now.AddDays(-retentionDays);
        var retired = await _dbContext.SigningKeyRingEntries
            .Where(entry => (entry.Status == SigningKeyStatus.Retired || entry.Status == SigningKeyStatus.Revoked)
                && entry.RetiredUtc != null
                && entry.RetiredUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (retired.Count == 0)
        {
            return;
        }

        _dbContext.SigningKeyRingEntries.RemoveRange(retired);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cleaned up {Count} retired or revoked signing keys.", retired.Count);
    }

    public SigningCredentials CreateSigningCredentials(SigningKeyRingEntry entry)
    {
        var privateBytes = Convert.FromBase64String(_protector.Unprotect(entry.PrivateKeyProtected));
        var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateBytes, out _);
        var key = new RsaSecurityKey(rsa) { KeyId = entry.Kid };
        return new SigningCredentials(key, entry.Algorithm);
    }

    private SigningKeyRingEntry CreateKeyEntry()
    {
        var options = _options.CurrentValue;
        using var rsa = RSA.Create(options.KeySize);
        var parameters = rsa.ExportParameters(true);

        var kid = GenerateKeyId();
        var now = DateTime.UtcNow;
        var jwk = new JsonWebKey
        {
            Kty = "RSA",
            Use = "sig",
            Alg = options.Algorithm,
            Kid = kid,
            N = Base64UrlEncoder.Encode(parameters.Modulus),
            E = Base64UrlEncoder.Encode(parameters.Exponent)
        };

        var publicJson = JsonSerializer.Serialize(jwk);
        var privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());

        return new SigningKeyRingEntry
        {
            Id = Guid.NewGuid(),
            Kid = kid,
            Status = SigningKeyStatus.Active,
            CreatedUtc = now,
            ActivatedUtc = now,
            RetireAfterUtc = null,
            NotBeforeUtc = now,
            Algorithm = options.Algorithm,
            KeyType = "RSA",
            PublicJwkJson = publicJson,
            PrivateKeyProtected = _protector.Protect(privateKey)
        };
    }

    private static string GenerateKeyId()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Base64UrlEncoder.Encode(bytes);
    }

    private DateTime? GetRetireAfterUtc(DateTime nowUtc)
    {
        var retentionDays = Math.Max(0, _options.CurrentValue.PreviousKeyRetentionDays);
        return retentionDays == 0 ? nowUtc : nowUtc.AddDays(retentionDays);
    }
}

public sealed record SigningKeyRotationResult(SigningKeyRingEntry NewActive, SigningKeyRingEntry? PreviousActive);

public enum SigningKeyRevokeResult
{
    Revoked = 0,
    ReplacedActive = 1,
    NotFound = 2
}
