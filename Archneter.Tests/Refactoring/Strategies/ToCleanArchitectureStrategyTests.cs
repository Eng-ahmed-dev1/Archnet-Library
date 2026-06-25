using Xunit;
using Archneter.Core.Models;
using Archneter.Generators.Refactoring.Strategies;
using FluentAssertions;
using Moq;
using Archneter.Generators.Infrastructure;

namespace Archneter.Tests.Refactoring.Strategies;

public class ToCleanArchitectureStrategyTests : IDisposable
{
    private readonly string _tempDir;

    public ToCleanArchitectureStrategyTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task ExecuteAsync_WithDryRun_DoesNotCreateAnyFiles()
    {
        var sut = new ToCleanArchitectureStrategy(new DryRunCliService());
        var options = new RefactorOptions
        {
            ProjectDirectory = _tempDir,
            ProjectName = "TestApp",
            DryRun = true,
            SkipBackup = true
        };
        var project = new AnalyzedProject { ProjectName = "TestApp" };

        await sut.ExecuteAsync(options, project);

        Directory.GetFileSystemEntries(_tempDir).Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithDryRun_PrintsDryRunMessages()
    {
        var sut = new ToCleanArchitectureStrategy(new DryRunCliService());
        var options = new RefactorOptions
        {
            ProjectDirectory = _tempDir,
            ProjectName = "TestApp",
            DryRun = true,
            SkipBackup = true
        };
        var project = new AnalyzedProject { ProjectName = "TestApp" };

        var sw = new StringWriter();
        Console.SetOut(sw);

        await sut.ExecuteAsync(options, project);

        sw.ToString().Should().Contain("[dry-run]");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyProject_CreatesExpected4Layers()
    {
        var sut = new ToCleanArchitectureStrategy(new DotnetCliService());
        var options = new RefactorOptions
        {
            ProjectDirectory = _tempDir,
            ProjectName = "TestApp",
            DryRun = false,
            SkipBackup = true
        };
        var project = new AnalyzedProject { ProjectName = "TestApp" };

        await sut.ExecuteAsync(options, project);

        var directories = Directory.GetDirectories(_tempDir).Select(Path.GetFileName).ToList();
        directories.Should().Contain("TestApp.Domain");
        directories.Should().Contain("TestApp.Application");
        directories.Should().Contain("TestApp.Infrastructure");
        directories.Should().Contain("TestApp.Api");
    }
}
