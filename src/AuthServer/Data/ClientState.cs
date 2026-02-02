namespace AuthServer.Data;

public sealed class ClientState
{
    public string ApplicationId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
