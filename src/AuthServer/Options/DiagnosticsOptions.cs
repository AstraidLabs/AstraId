namespace AuthServer.Options;

public sealed class DiagnosticsOptions
{
    public const string SectionName = "Diagnostics";

    public bool ExposeToAdmins { get; set; } = true;

    public bool StoreErrorLogs { get; set; } = true;

    public int MaxStoredDays { get; set; } = 14;
}
