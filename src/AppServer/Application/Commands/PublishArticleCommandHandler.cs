using AstraId.Contracts;
using AppServer.Application.Jobs;
using AppServer.Infrastructure.Events;
using Hangfire;
using MediatR;

namespace AppServer.Application.Commands;

public sealed class PublishArticleCommandHandler : IRequestHandler<PublishArticleCommand, PublishArticleResult>
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IEventPublisher _eventPublisher;

    public PublishArticleCommandHandler(IBackgroundJobClient backgroundJobs, IEventPublisher eventPublisher)
    {
        _backgroundJobs = backgroundJobs;
        _eventPublisher = eventPublisher;
    }

    public async Task<PublishArticleResult> Handle(PublishArticleCommand request, CancellationToken cancellationToken)
    {
        _backgroundJobs.Enqueue<GenerateThumbnailJob>(job => job.ExecuteAsync(request.ArticleId));

        await _eventPublisher.PublishAsync(new AppEvent(
            Type: "article.published",
            TenantId: request.TenantId,
            UserId: request.UserId,
            EntityId: request.ArticleId,
            OccurredAt: DateTimeOffset.UtcNow,
            Payload: new { request.ArticleId }), cancellationToken);

        return new PublishArticleResult(request.ArticleId, "published");
    }
}
