namespace AuthServer.Data;

public class ApiEndpoint
{
    public Guid Id { get; set; }
    public Guid ApiResourceId { get; set; }
    public ApiResource? ApiResource { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsDeprecated { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Tags { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<EndpointPermission> EndpointPermissions { get; set; } = new List<EndpointPermission>();
}
