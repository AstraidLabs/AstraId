using System.Security.Cryptography.X509Certificates;
using AuthServer.Options;
using AuthServer.Services.Cryptography;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Governance;

public sealed record EncryptionKeyStatus(
    bool Enabled,
    string Source,
    string? Thumbprint,
    string? Subject,
    DateTime? NotBeforeUtc,
    DateTime? NotAfterUtc);

public sealed class EncryptionKeyStatusService
{
    private readonly IOptions<AuthServerCertificateOptions> _options;
    private readonly IHostEnvironment _environment;

    public EncryptionKeyStatusService(IOptions<AuthServerCertificateOptions> options, IHostEnvironment environment)
    {
        _options = options;
        _environment = environment;
    }

    public EncryptionKeyStatus GetStatus()
    {
        var descriptor = _options.Value.Encryption;
        var certificate = CertificateLoader.TryLoadCertificate(descriptor);
        if (certificate is null)
        {
            if (_environment.IsDevelopment())
            {
                return new EncryptionKeyStatus(
                    Enabled: true,
                    Source: "Development",
                    Thumbprint: null,
                    Subject: "Development certificate",
                    NotBeforeUtc: null,
                    NotAfterUtc: null);
            }

            return new EncryptionKeyStatus(
                Enabled: false,
                Source: "Missing",
                Thumbprint: null,
                Subject: null,
                NotBeforeUtc: null,
                NotAfterUtc: null);
        }

        return new EncryptionKeyStatus(
            Enabled: true,
            Source: ResolveSource(descriptor),
            Thumbprint: certificate.Thumbprint,
            Subject: certificate.Subject,
            NotBeforeUtc: certificate.NotBefore.ToUniversalTime(),
            NotAfterUtc: certificate.NotAfter.ToUniversalTime());
    }

    private static string ResolveSource(CertificateDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return "Unknown";
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Base64))
        {
            return "Base64";
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Path))
        {
            return "File";
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Thumbprint))
        {
            return "Store";
        }

        return "Unknown";
    }
}
