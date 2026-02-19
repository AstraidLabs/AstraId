using Microsoft.Extensions.Logging;

namespace AstraId.Logging.Options;

public sealed class AstraLoggingOptions
{
    public const string SectionName = "AstraLogging";

    public string Mode { get; set; } = "Production";

    public bool RedactionEnabled { get; set; } = true;

    public StreamOptions Application { get; set; } = new();

    public StreamOptions DeveloperDiagnostics { get; set; } = new() { Enabled = false, MinimumLevel = LogLevel.Debug };

    public StreamOptions SecurityAudit { get; set; } = new() { Enabled = true, MinimumLevel = LogLevel.Information };

    public RequestLoggingOptions RequestLogging { get; set; } = new();

    public bool IsDevelopmentLike => string.Equals(Mode, "Development", StringComparison.OrdinalIgnoreCase);

    public sealed class StreamOptions
    {
        public bool Enabled { get; set; } = true;
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    }

    public sealed class RequestLoggingOptions
    {
        public bool Enabled { get; set; } = true;
        public bool IncludeQueryString { get; set; }
        public bool IncludeBody { get; set; }
        public int MaxBodyLength { get; set; } = 2048;
        public string[] SafeBodyPathAllowList { get; set; } = [];
        public string[] BodyPathBlockListPrefixes { get; set; } = ["/connect/", "/auth/", "/admin/", "/account/", "/hubs/"];
    }
}
