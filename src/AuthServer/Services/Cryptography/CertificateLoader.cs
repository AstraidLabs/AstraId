using System.Security.Cryptography.X509Certificates;
using AuthServer.Options;

namespace AuthServer.Services.Cryptography;

public static class CertificateLoader
{
    public static X509Certificate2? TryLoadCertificate(CertificateDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Base64))
        {
            var bytes = Convert.FromBase64String(descriptor.Base64);
            return new X509Certificate2(bytes, descriptor.Password, X509KeyStorageFlags.MachineKeySet);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Path))
        {
            return new X509Certificate2(descriptor.Path, descriptor.Password, X509KeyStorageFlags.MachineKeySet);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Thumbprint))
        {
            return FindByThumbprint(
                descriptor.Thumbprint,
                descriptor.StoreName,
                descriptor.StoreLocation);
        }

        return null;
    }

    private static X509Certificate2? FindByThumbprint(string thumbprint, string? storeName, string? storeLocation)
    {
        var resolvedStoreName = Enum.TryParse(storeName, true, out StoreName parsedStoreName)
            ? parsedStoreName
            : StoreName.My;
        var resolvedStoreLocation = Enum.TryParse(storeLocation, true, out StoreLocation parsedStoreLocation)
            ? parsedStoreLocation
            : StoreLocation.CurrentUser;

        using var store = new X509Store(resolvedStoreName, resolvedStoreLocation);
        store.Open(OpenFlags.ReadOnly);

        var certificates = store.Certificates.Find(
            X509FindType.FindByThumbprint,
            thumbprint,
            validOnly: false);

        return certificates.Count > 0 ? certificates[0] : null;
    }
}
