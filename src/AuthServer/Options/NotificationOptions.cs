namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for notification.
/// </summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";
    public int DispatcherBatchSize { get; set; } = 50;
    public int DispatcherIntervalSeconds { get; set; } = 15;
    public int MaxAttempts { get; set; } = 5;
    public int BaseBackoffSeconds { get; set; } = 30;
    public int MaxBackoffSeconds { get; set; } = 3600;
}
