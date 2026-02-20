using MediatR;

namespace AppServer.Application.Commands;

/// <summary>
/// Provides publish article command functionality.
/// </summary>
public sealed record PublishArticleCommand(string ArticleId, string? TenantId, string? UserId) : IRequest<PublishArticleResult>;

/// <summary>
/// Provides publish article result functionality.
/// </summary>
public sealed record PublishArticleResult(string ArticleId, string Status);
