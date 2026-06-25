using Xunit;
using Archneter.Cli.Commands;
using Archneter.Cli.Models;
using Archneter.Core.Enums;
using Archneter.Core.Models;
using Archneter.Generators.Refactoring;
using FluentAssertions;
using Moq;
using Archneter.Generators.Infrastructure;

namespace Archneter.Tests.Commands;

public class RefactorCommandTests : IDisposable
{
    private readonly Mock<ProjectAnalyzer> _analyzerMock;
    private readonly Mock<RefactoringStrategyFactory> _factoryMock;
    private readonly Mock<IRefactoringStrategy> _strategyMock;
    private readonly RefactorCommand _sut;
    private readonly string _tempDir;

    public RefactorCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _analyzerMock = new Mock<ProjectAnalyzer>();
        _factoryMock = new Mock<RefactoringStrategyFactory>(new DotnetCliService(), new DryRunCliService());
        _strategyMock = new Mock<IRefactoringStrategy>();

        _analyzerMock
            .Setup(x => x.Analyze(It.IsAny<string>()))
            .Returns(new AnalyzedProject { ProjectName = "TestApp" });

        _factoryMock
            .Setup(x => x.Create(It.IsAny<ArchitectureType>(), It.IsAny<bool>()))
            .Returns(_strategyMock.Object);

        _sut = new RefactorCommand(_analyzerMock.Object, _factoryMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingToFlag_PrintsErrorAndReturns()
    {
        var context = new CommandContext();
        var sw = new StringWriter();
        Console.SetOut(sw);

        await _sut.ExecuteAsync(context);

        sw.ToString().Should().Contain("Missing required flag");
        _analyzerMock.Verify(x => x.Analyze(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownArchitecture_PrintsErrorAndReturns()
    {
        var context = new CommandContext { Options = new() { { "--to", "unknown" } } };
        var sw = new StringWriter();
        Console.SetOut(sw);

        await _sut.ExecuteAsync(context);

        sw.ToString().Should().Contain("Unknown architecture");
        _analyzerMock.Verify(x => x.Analyze(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentDir_PrintsErrorAndReturns()
    {
        var context = new CommandContext 
        { 
            Options = new() { { "--to", "clean" }, { "--dir", "C:\\invalid_directory_that_does_not_exist" } } 
        };
        var sw = new StringWriter();
        Console.SetOut(sw);

        await _sut.ExecuteAsync(context);

        sw.ToString().Should().Contain("Directory not found");
        _analyzerMock.Verify(x => x.Analyze(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithDryRunFlag_DoesNotPromptUser()
    {
        var context = new CommandContext 
        { 
            Options = new() { { "--to", "clean" }, { "--dir", _tempDir } },
            Flags = new() { "--dry-run" }
        };
        
        var sr = new StringReader(""); // Empty input, would crash if it prompts
        Console.SetIn(sr);

        await _sut.ExecuteAsync(context);

        _strategyMock.Verify(x => x.ExecuteAsync(It.Is<RefactorOptions>(o => o.DryRun == true), It.IsAny<AnalyzedProject>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithForceFlag_DoesNotPromptUser()
    {
        var context = new CommandContext 
        { 
            Options = new() { { "--to", "clean" }, { "--dir", _tempDir } },
            Flags = new() { "--force" }
        };
        
        var sr = new StringReader(""); // Empty input
        Console.SetIn(sr);

        await _sut.ExecuteAsync(context);

        _strategyMock.Verify(x => x.ExecuteAsync(It.Is<RefactorOptions>(o => o.Force == true), It.IsAny<AnalyzedProject>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidArgs_CallsAnalyzerOnce()
    {
        var context = new CommandContext 
        { 
            Options = new() { { "--to", "clean" }, { "--dir", _tempDir } },
            Flags = new() { "--force" }
        };

        await _sut.ExecuteAsync(context);

        _analyzerMock.Verify(x => x.Analyze(_tempDir), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidArgs_CallsStrategyExecuteOnce()
    {
        var context = new CommandContext 
        { 
            Options = new() { { "--to", "clean" }, { "--dir", _tempDir } },
            Flags = new() { "--force" }
        };

        await _sut.ExecuteAsync(context);

        _strategyMock.Verify(x => x.ExecuteAsync(It.IsAny<RefactorOptions>(), It.IsAny<AnalyzedProject>()), Times.Once);
    }

    [Theory]
    [InlineData("clean", ArchitectureType.CleanArchitecture)]
    [InlineData("verticalslice", ArchitectureType.VerticalSlice)]
    [InlineData("modularmonolith", ArchitectureType.ModularMonolith)]
    [InlineData("microservices", ArchitectureType.Microservices)]
    [InlineData("n-tier", ArchitectureType.NTier)]
    [InlineData("ntier", ArchitectureType.NTier)]
    public async Task ExecuteAsync_WithAllArchitectureAliases_ResolvesCorrectly(string alias, ArchitectureType expectedArch)
    {
        var context = new CommandContext 
        { 
            Options = new() { { "--to", alias }, { "--dir", _tempDir } },
            Flags = new() { "--force" }
        };

        await _sut.ExecuteAsync(context);

        _factoryMock.Verify(x => x.Create(expectedArch, It.IsAny<bool>()), Times.Once);
        _strategyMock.Verify(x => x.ExecuteAsync(It.Is<RefactorOptions>(o => o.TargetArchitecture == expectedArch), It.IsAny<AnalyzedProject>()), Times.Once);
    }
}
