using System.Text.Json;

namespace AuthServer.Services;

/// <summary>
/// Provides admin ui manifest service functionality.
/// </summary>
public sealed class AdminUiManifestService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AdminUiManifestService> _logger;
    private readonly object _sync = new();
    private DateTimeOffset? _lastWrite;
    private IReadOnlyDictionary<string, ViteManifestEntry>? _manifest;

    public AdminUiManifestService(IWebHostEnvironment environment, ILogger<AdminUiManifestService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public ViteManifestEntry? GetEntry(string entryName)
    {
        EnsureManifestLoaded();
        return _manifest is null || !_manifest.TryGetValue(entryName, out var entry) ? null : entry;
    }

    public string GetAssetPath(string asset)
    {
        return $"/admin/{asset.TrimStart('/')}";
    }

    private void EnsureManifestLoaded()
    {
        var manifestPath = GetManifestPath();
        if (manifestPath is null)
        {
            return;
        }

        var info = new FileInfo(manifestPath);
        var lastWriteTime = info.Exists ? info.LastWriteTimeUtc : (DateTimeOffset?)null;

        if (_manifest is not null && lastWriteTime.HasValue && lastWriteTime == _lastWrite)
        {
            return;
        }

        lock (_sync)
        {
            if (_manifest is not null && lastWriteTime.HasValue && lastWriteTime == _lastWrite)
            {
                return;
            }

            try
            {
                if (!info.Exists)
                {
                    _logger.LogWarning("Admin UI manifest not found at {ManifestPath}.", manifestPath);
                    _manifest = null;
                    _lastWrite = null;
                    return;
                }

                var json = File.ReadAllText(info.FullName);
                _manifest = JsonSerializer.Deserialize<Dictionary<string, ViteManifestEntry>>(json)
                    ?? new Dictionary<string, ViteManifestEntry>();
                _lastWrite = lastWriteTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load admin UI manifest.");
                _manifest = null;
                _lastWrite = null;
            }
        }
    }

    private string? GetManifestPath()
    {
        var root = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        var direct = Path.Combine(root, "admin-ui", "manifest.json");
        if (File.Exists(direct))
        {
            return direct;
        }

        var vite = Path.Combine(root, "admin-ui", ".vite", "manifest.json");
        return File.Exists(vite) ? vite : direct;
    }

    /// <summary>
    /// Provides vite manifest entry functionality.
    /// </summary>
    public sealed class ViteManifestEntry
    {
        public string File { get; set; } = string.Empty;
        public IReadOnlyList<string>? Css { get; set; }
        public IReadOnlyList<string>? Imports { get; set; }
        public bool IsEntry { get; set; }
    }
}
