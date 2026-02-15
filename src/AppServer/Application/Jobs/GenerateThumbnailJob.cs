namespace AppServer.Application.Jobs;

public sealed class GenerateThumbnailJob
{
    private readonly ILogger<GenerateThumbnailJob> _logger;

    public GenerateThumbnailJob(ILogger<GenerateThumbnailJob> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(string articleId)
    {
        _logger.LogInformation("Generating thumbnail for article {ArticleId}.", articleId);
        return Task.CompletedTask;
    }
}
