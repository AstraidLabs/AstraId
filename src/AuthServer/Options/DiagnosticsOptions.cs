namespace AuthServer.Options;

/// <summary>
/// Provides configuration options for diagnostics.
/// </summary>
public sealed class DiagnosticsOptions
{
    public const string SectionName = "Diagnostics";

    public bool ExposeToAdmins { get; set; } = true;

    public bool StoreErrorLogs { get; set; } = true;

    public int MaxStoredDays { get; set; } = 14;

    public bool StoreDetailedExceptionDataInProduction { get; set; } = false;
}
