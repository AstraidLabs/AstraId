namespace AuthServer.Data;

public sealed class ConsumedRefreshToken
{
    public string TokenId { get; set; } = string.Empty;
    public DateTime ConsumedUtc { get; set; }
    public DateTime? ExpiresUtc { get; set; }
}
