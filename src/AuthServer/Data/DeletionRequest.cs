namespace AuthServer.Data;

public enum DeletionRequestStatus
{
    Pending = 0,
    Approved = 1,
    Cancelled = 2,
    Executed = 3,
    Rejected = 4
}

public sealed class DeletionRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime RequestedUtc { get; set; }
    public DeletionRequestStatus Status { get; set; } = DeletionRequestStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ExecutedUtc { get; set; }
    public DateTime? CancelUtc { get; set; }
    public string? Reason { get; set; }
    public DateTime CooldownUntilUtc { get; set; }

    public ApplicationUser? User { get; set; }
}
