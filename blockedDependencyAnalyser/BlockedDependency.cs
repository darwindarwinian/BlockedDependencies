public record BlockedDependency()
{
    public string PackageName { get; set;}
    public string? MinimumVersion {get; set; }
    public string? MaximumVersion {get; set; }
}