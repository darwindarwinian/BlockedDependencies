namespace BlockedDependencies;

public record BlockedDependency()
{
    public required string PackageName { get; set; }
    public string? BlockFromVersion { get; set; }
    public string? BlockToVersion { get; set; }
}
