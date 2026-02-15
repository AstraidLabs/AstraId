using MediatR;

namespace AppServer.Application.Commands;

public sealed record PublishArticleCommand(string ArticleId, string? TenantId, string? UserId) : IRequest<PublishArticleResult>;

public sealed record PublishArticleResult(string ArticleId, string Status);
