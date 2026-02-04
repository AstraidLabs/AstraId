using AuthServer.Services.Admin;
using AuthServer.Services.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers.Admin;

[ApiController]
[Route("admin/api/clients")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminClientsController : ControllerBase
{
    private readonly IAdminClientService _clientService;

    public AdminClientsController(IAdminClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminClientListItem>>> GetClients(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _clientService.GetClientsAsync(search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminClientDetail>> GetClient(string id, CancellationToken cancellationToken)
    {
        var client = await _clientService.GetClientAsync(id, cancellationToken);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<AdminClientSecretResponse>> CreateClient(
        [FromBody] AdminClientCreateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clientService.CreateClientAsync(request, cancellationToken);
            var response = new AdminClientSecretResponse(result.Client, result.ClientSecret);
            return CreatedAtAction(nameof(GetClient), new { id = result.Client.Id }, response);
        }
        catch (AdminClientValidationException exception)
        {
            return ValidationProblem(exception.ToProblemDetails());
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminClientDetail>> UpdateClient(
        string id,
        [FromBody] AdminClientUpdateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clientService.UpdateClientAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (AdminClientValidationException exception)
        {
            return ValidationProblem(exception.ToProblemDetails());
        }
    }

    [HttpPost("{id}/rotate-secret")]
    public async Task<ActionResult<AdminClientSecretResponse>> RotateSecret(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clientService.RotateSecretAsync(id, cancellationToken);
            return result is null
                ? NotFound()
                : Ok(new AdminClientSecretResponse(result.Client, result.ClientSecret));
        }
        catch (AdminClientValidationException exception)
        {
            return ValidationProblem(exception.ToProblemDetails());
        }
    }

    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<AdminClientDetail>> ToggleClient(
        string id,
        [FromBody] AdminClientToggleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _clientService.SetEnabledAsync(id, request.Enabled, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(string id, CancellationToken cancellationToken)
    {
        var deleted = await _clientService.DeleteClientAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
