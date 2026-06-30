using Xunit;
using Archneter.Generators.Refactoring;
using FluentAssertions;

namespace Archneter.Tests.Refactoring;

public class NamespaceRewriterTests : IDisposable
{
    private readonly string _tempDir;

    public NamespaceRewriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTempFile(string content)
    {
        var path = Path.Combine(_tempDir, Guid.NewGuid() + ".cs");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Rewrite_FileScopedNamespace_ReplacesCorrectly()
    {
        var path = CreateTempFile("namespace OldApp.Services;\npublic class Foo {}");
        var map = new Dictionary<string, List<string>>();
        NamespaceRewriter.Rewrite(path, "NewApp.Application", map);
        var content = File.ReadAllText(path);
        content.Should().Contain("namespace NewApp.Application;");
    }

    [Fact]
    public void Rewrite_BlockScopedNamespace_ReplacesCorrectly()
    {
        var path = CreateTempFile("namespace OldApp.Services {\npublic class Foo {}\n}");
        var map = new Dictionary<string, List<string>>();
        NamespaceRewriter.Rewrite(path, "NewApp.Application", map);
        var content = File.ReadAllText(path);
        content.Should().Contain("namespace NewApp.Application {");
    }

    [Fact]
    public void Rewrite_UsingDirective_ReplacesOldRootNamespace()
    {
        var path = CreateTempFile("using OldApp.Core;\nnamespace Test;");
        var map = new Dictionary<string, List<string>> { { "OldApp.Core", new List<string> { "NewApp.Core" } } };
        NamespaceRewriter.Rewrite(path, "NewApp", map);
        var content = File.ReadAllText(path);
        content.Should().Contain("using NewApp.Core;");
    }

    [Fact]
    public void Rewrite_UsingDirective_DoesNotReplaceUnrelatedNamespace()
    {
        var path = CreateTempFile("using System.Linq;\nnamespace Test;");
        var map = new Dictionary<string, List<string>> { { "OldApp.Core", new List<string> { "NewApp.Core" } } };
        NamespaceRewriter.Rewrite(path, "NewApp", map);
        var content = File.ReadAllText(path);
        content.Should().Contain("using System.Linq;");
    }

    [Fact]
    public void Rewrite_NonExistentFile_DoesNotThrow()
    {
        var map = new Dictionary<string, List<string>>();
        var action = () => NamespaceRewriter.Rewrite(Path.Combine(_tempDir, "none.cs"), "New", map);
        action.Should().NotThrow();
    }

    [Fact]
    public void BuildNamespace_WithSubFolder_ReturnsCorrectString()
    {
        var result = NamespaceRewriter.BuildNamespace("App", "Domain", "Entities");
        result.Should().Be("App.Domain.Entities");
    }

    [Fact]
    public void BuildNamespace_WithoutSubFolder_ReturnsCorrectString()
    {
        var result = NamespaceRewriter.BuildNamespace("App", "Domain", null);
        result.Should().Be("App.Domain");
    }

    [Fact]
    public void BuildNamespace_WithNullSubFolder_ReturnsCorrectString()
    {
        var result = NamespaceRewriter.BuildNamespace("App", "Domain", string.Empty);
        result.Should().Be("App.Domain");
    }
}
