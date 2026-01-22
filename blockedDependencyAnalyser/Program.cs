using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace BlockedDependencies;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BlockedDependencyAnalyser : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NC0001";
    private static readonly LocalizableString Title = "Blocked Dependency Detected";
    private static readonly LocalizableString MessageFormat = "Project contains blocked dependency. This version is prohibited.";
    private const string Category = "Compatibility";
    
    private static List<BlockedDependency> BlockedDependencies = [
        new BlockedDependency() {PackageName = "Newtonsoft.Json", BlockFromVersion = "13.0.0", BlockToVersion = null}
    ];
        
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Warns when a project has blocked dependencies.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(CheckProjectDependencies);
    }

    private void CheckProjectDependencies(CompilationAnalysisContext context)
    {
        var csprojPath = FindCsprojFile(context);
        if (csprojPath == null)
            return;

        if (HasBlockedDependency(csprojPath, BlockedDependencies))
        {
            var diagnostic = Diagnostic.Create(Rule, Location.None);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private string? FindCsprojFile(CompilationAnalysisContext context)
    {
        var sourceTree = context.Compilation.SyntaxTrees.FirstOrDefault();
        if (sourceTree?.FilePath == null)
            return null;

        var directory = Path.GetDirectoryName(sourceTree.FilePath);
        if (string.IsNullOrWhiteSpace(directory))
            return null;

        try
        {
            // Search for .csproj file in the current directory and parent directories
            var currentDir = new DirectoryInfo(directory);
            while (currentDir != null)
            {
                var csprojFiles = currentDir.GetFiles("*.csproj");
                if (csprojFiles.Length > 0)
                    return csprojFiles[0].FullName;

                currentDir = currentDir.Parent;
            }
        }
        catch
        {
            // If there's any error accessing the filesystem, return null gracefully
        }

        return null;
    }

    private bool HasBlockedDependency(string csprojPath, List<BlockedDependency> blockedDependencies)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            var ns = doc.Root?.Name.NamespaceName ?? "http://schemas.microsoft.com/developer/msbuild/2003";
            var xns = XNamespace.Get(ns);

            // Check PackageReference items for Newtonsoft.Json v13+
            var packageReferences = doc.Descendants(xns + "PackageReference");
            foreach (var reference in packageReferences)
            {
                foreach (var blocked in blockedDependencies)
                {
                    var includeAttr = reference.Attribute("Include")?.Value ?? "";
                    if (includeAttr.Equals(blocked.PackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        var versionAttr = reference.Attribute("Version")?.Value ?? "";
                        var isBlocked = (blocked.BlockFromVersion == null || IsVersionGreaterOrEqual(versionAttr, blocked.BlockFromVersion))
                            && (blocked.BlockToVersion == null || IsVersionLessThanOrEqual(versionAttr, blocked.BlockToVersion));
                        if (isBlocked)
                            return true;
                    }
                }
            }

            // Also check for legacy Reference elements
            var references = doc.Descendants(xns + "Reference");
            foreach (var reference in references)
            {
                foreach (var blocked in blockedDependencies)
                {
                    var includeAttr = reference.Attribute("Include")?.Value ?? "";
                    if (includeAttr.StartsWith(blocked.PackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        var versionMatch = System.Text.RegularExpressions.Regex.Match(includeAttr, @"Version=(\d+\.\d+\.\d+\.\d+)");
                        if (versionMatch.Success && Version.TryParse(versionMatch.Groups[1].Value, out var version))
                        {
                            var isBlocked = (blocked.BlockFromVersion == null || IsVersionGreaterOrEqual(version.ToString(), blocked.BlockFromVersion))
                                && (blocked.BlockToVersion == null || IsVersionLessThanOrEqual(version.ToString(), blocked.BlockToVersion));
                            if (isBlocked)
                                return true;
                        }
                    }
                }
            }

            return false;
        }
        catch (Exception)
        {
            // If there's an error reading the csproj file, don't report a diagnostic
            return false;
        }
    }

    private bool IsVersionGreaterOrEqual(string versionString, string minimumVersion)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            return false;

        if (Version.TryParse(versionString, out var version) && Version.TryParse(minimumVersion, out var minVersion))
            return version >= minVersion;

        return false;
    }

    private bool IsVersionLessThanOrEqual(string versionString, string minimumVersion)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            return false;

        if (Version.TryParse(versionString, out var version) && Version.TryParse(minimumVersion, out var minVersion))
            return version <= minVersion;

        return false;
    }
}
