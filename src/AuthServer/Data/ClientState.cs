namespace AuthServer.Data;

public sealed class ClientState
{
    public string ApplicationId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool SystemManaged { get; set; }
    public string? Profile { get; set; }
    public string? AppliedPresetId { get; set; }
    public int? AppliedPresetVersion { get; set; }
    public string? OverridesJson { get; set; }
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
