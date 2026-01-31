namespace AuthServer.Data;

public class ApiResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ApiKeyHash { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<ApiEndpoint> Endpoints { get; set; } = new List<ApiEndpoint>();
}
