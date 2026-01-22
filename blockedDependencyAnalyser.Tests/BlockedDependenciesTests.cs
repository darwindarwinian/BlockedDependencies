using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Xunit;

namespace BlockedDependencyAnalyser.Tests;

public class BlockedDependencyAnalyserTests
{
    [Fact]
    public void ShouldDetectNewtonsoftJsonV13PackageReference()
    {
        // Arrange - Create a temporary directory with .csproj file
        var tempDir = Path.Combine(Path.GetTempPath(), $"TestProject_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Write .csproj file with Newtonsoft.Json v13.0.3
            var csprojPath = Path.Combine(tempDir, "TestProject.csproj");
            var csprojContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                  </ItemGroup>
                </Project>
                """;
            File.WriteAllText(csprojPath, csprojContent);

            // Write source file
            var sourcePath = Path.Combine(tempDir, "Program.cs");
            var sourceContent = "public class TestClass { }";
            File.WriteAllText(sourcePath, sourceContent);

            // Act
            var diagnostics = RunAnalyzer(tempDir, sourceContent);

            // Assert
            Assert.NotEmpty(diagnostics);
            Assert.Contains(diagnostics, d => d.Id == BlockedDependencies.BlockedDependencyAnalyser.DiagnosticId);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ShouldNotReportDiagnosticForNewtonsoftJsonV12()
    {
        // Arrange - Create a temporary directory with .csproj file
        var tempDir = Path.Combine(Path.GetTempPath(), $"TestProject_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Write .csproj file with Newtonsoft.Json v12.0.3 (below v13)
            var csprojPath = Path.Combine(tempDir, "TestProject.csproj");
            var csprojContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Newtonsoft.Json" Version="12.0.3" />
                  </ItemGroup>
                </Project>
                """;
            File.WriteAllText(csprojPath, csprojContent);

            // Write source file
            var sourcePath = Path.Combine(tempDir, "Program.cs");
            var sourceContent = "public class TestClass { }";
            File.WriteAllText(sourcePath, sourceContent);

            // Act
            var diagnostics = RunAnalyzer(tempDir, sourceContent);

            // Assert
            Assert.Empty(diagnostics.Where(d => d.Id == BlockedDependencies.BlockedDependencyAnalyser.DiagnosticId));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ShouldNotReportDiagnosticWithoutNewtonsoftJson()
    {
        // Arrange - Create a temporary directory with .csproj file
        var tempDir = Path.Combine(Path.GetTempPath(), $"TestProject_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Write .csproj file without Newtonsoft.Json
            var csprojPath = Path.Combine(tempDir, "TestProject.csproj");
            var csprojContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="System.Text.Json" Version="10.0.0" />
                  </ItemGroup>
                </Project>
                """;
            File.WriteAllText(csprojPath, csprojContent);

            // Write source file
            var sourcePath = Path.Combine(tempDir, "Program.cs");
            var sourceContent = "public class TestClass { }";
            File.WriteAllText(sourcePath, sourceContent);

            // Act
            var diagnostics = RunAnalyzer(tempDir, sourceContent);

            // Assert
            Assert.Empty(diagnostics.Where(d => d.Id == BlockedDependencies.BlockedDependencyAnalyser.DiagnosticId));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ShouldDetectNewtonsoftJsonV13LegacyReference()
    {
        // Arrange - Create a temporary directory with .csproj file
        var tempDir = Path.Combine(Path.GetTempPath(), $"TestProject_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Write .csproj file with legacy Reference for Newtonsoft.Json v13.0.0.0
            var csprojPath = Path.Combine(tempDir, "TestProject.csproj");
            var csprojContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Reference Include="Newtonsoft.Json, Version=13.0.0.0" />
                  </ItemGroup>
                </Project>
                """;
            File.WriteAllText(csprojPath, csprojContent);

            // Write source file
            var sourcePath = Path.Combine(tempDir, "Program.cs");
            var sourceContent = "public class TestClass { }";
            File.WriteAllText(sourcePath, sourceContent);

            // Act
            var diagnostics = RunAnalyzer(tempDir, sourceContent);

            // Assert
            Assert.NotEmpty(diagnostics);
            Assert.Contains(diagnostics, d => d.Id == BlockedDependencies.BlockedDependencyAnalyser.DiagnosticId);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ShouldNotReportDiagnosticForLegacyReferenceV12()
    {
        // Arrange - Create a temporary directory with .csproj file
        var tempDir = Path.Combine(Path.GetTempPath(), $"TestProject_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Write .csproj file with legacy Reference for Newtonsoft.Json v12.0.3.0
            var csprojPath = Path.Combine(tempDir, "TestProject.csproj");
            var csprojContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Reference Include="Newtonsoft.Json, Version=12.0.3.0" />
                  </ItemGroup>
                </Project>
                """;
            File.WriteAllText(csprojPath, csprojContent);

            // Write source file
            var sourcePath = Path.Combine(tempDir, "Program.cs");
            var sourceContent = "public class TestClass { }";
            File.WriteAllText(sourcePath, sourceContent);

            // Act
            var diagnostics = RunAnalyzer(tempDir, sourceContent);

            // Assert
            Assert.Empty(diagnostics.Where(d => d.Id == BlockedDependencies.BlockedDependencyAnalyser.DiagnosticId));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Runs the analyzer on source code and returns any diagnostics
    /// </summary>
    private IEnumerable<Diagnostic> RunAnalyzer(string projectDir, string sourceCode)
    {
        // Create a syntax tree from the source code
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: Path.Combine(projectDir, "Program.cs"));

        // Create a compilation
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        // Create and run the analyzer
        var analyzer = new BlockedDependencies.BlockedDependencyAnalyser();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
        return diagnostics;
    }
}
