namespace AuthServer.Data;

public class EndpointPermission
{
    public Guid EndpointId { get; set; }
    public ApiEndpoint? Endpoint { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}
