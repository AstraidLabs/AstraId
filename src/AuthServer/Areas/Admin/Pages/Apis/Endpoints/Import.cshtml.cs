using System.Text.Json;
using AuthServer.Models;
using AuthServer.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthServer.Areas.Admin.Pages.Apis.Endpoints;

[Authorize(Policy = "AdminOnly")]
public class ImportModel : PageModel
{
    private readonly IAdminApiResourceService _apiResourceService;
    private readonly IAdminEndpointService _endpointService;

    public ImportModel(IAdminApiResourceService apiResourceService, IAdminEndpointService endpointService)
    {
        _apiResourceService = apiResourceService;
        _endpointService = endpointService;
    }

    [BindProperty(SupportsGet = true, Name = "apiId")]
    public Guid ApiId { get; set; }

    [BindProperty]
    public string? Payload { get; set; }

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var apiResource = await _apiResourceService.GetApiResourceAsync(ApiId, cancellationToken);
        if (apiResource is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(Payload))
        {
            StatusMessage = "Payload is required.";
            return Page();
        }

        try
        {
            var endpoints = JsonSerializer.Deserialize<List<ApiEndpointSyncDto>>(Payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<ApiEndpointSyncDto>();

            var result = await _endpointService.SyncEndpointsAsync(apiResource, endpoints, cancellationToken);
            StatusMessage = $"Imported: created {result.CreatedCount}, updated {result.UpdatedCount}, deactivated {result.DeactivatedCount}.";
        }
        catch (JsonException)
        {
            StatusMessage = "Invalid JSON payload.";
        }

        return Page();
    }
}
