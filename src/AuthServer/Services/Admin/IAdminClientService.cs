using AuthServer.Services.Admin.Models;

namespace AuthServer.Services.Admin;

/// <summary>
/// Defines the contract for admin client service.
/// </summary>
public interface IAdminClientService
{
    Task<PagedResult<AdminClientListItem>> GetClientsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminClientDetail?> GetClientAsync(string id, CancellationToken cancellationToken);
    Task<AdminClientSaveResponse> CreateClientAsync(AdminClientCreateRequest request, CancellationToken cancellationToken);
    Task<AdminClientSaveResponse?> UpdateClientAsync(string id, AdminClientUpdateRequest request, CancellationToken cancellationToken);
    Task<AdminClientPreviewResponse> PreviewAsync(AdminClientCreateRequest request, CancellationToken cancellationToken);
    Task<AdminClientSecretResult?> RotateSecretAsync(string id, CancellationToken cancellationToken);
    Task<AdminClientDetail?> SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken);
    Task<bool> DeleteClientAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminClientScopeItem>> GetScopesAsync(CancellationToken cancellationToken);
}
