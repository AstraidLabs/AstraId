namespace AuthServer.Services.Admin.Models;

public sealed record FindingDto(
    string Severity,
    string Code,
    string Title,
    string Message,
    string? Field = null,
    IReadOnlyList<string>? Tags = null,
    string? DocsUrl = null,
    string? RecommendedFix = null);

public sealed record AdminClientSaveResponse(AdminClientDetail Client, IReadOnlyList<FindingDto> Findings, string? ClientSecret = null);

public sealed record AdminClientPreviewResponse(AdminClientEffectiveConfig EffectiveConfig, IReadOnlyList<FindingDto> Findings);
