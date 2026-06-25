using Xunit;
using Archneter.Core.Models;
using Archneter.Generators.Refactoring;
using FluentAssertions;

namespace Archneter.Tests.Refactoring;

public class ProjectAnalyzerTests : IDisposable
{
    private readonly ProjectAnalyzer _sut;
    private readonly string _tempDir;

    public ProjectAnalyzerTests()
    {
        _sut = new ProjectAnalyzer();
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void CreateFile(string relativePath, string content = "")
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    [Fact]
    public void Analyze_WithControllerFile_ClassifiesAsPresentation()
    {
        CreateFile("WeatherController.cs", "public class WeatherController {}");
        var result = _sut.Analyze(_tempDir);
        result.Files.Should().ContainSingle(f => f.FileName == "WeatherController.cs" && f.TargetLayer == ProjectLayer.Presentation);
    }

    [Fact]
    public void Analyze_WithServiceFile_ClassifiesAsApplication()
    {
        CreateFile("OrderService.cs", "public class OrderService {}");
        var result = _sut.Analyze(_tempDir);
        result.Files.Should().ContainSingle(f => f.FileName == "OrderService.cs" && f.TargetLayer == ProjectLayer.Application);
    }

    [Fact]
    public void Analyze_WithRepositoryFile_ClassifiesAsInfrastructure()
    {
        CreateFile("OrderRepository.cs", "public class OrderRepository {}");
        var result = _sut.Analyze(_tempDir);
        result.Files.Should().ContainSingle(f => f.FileName == "OrderRepository.cs" && f.TargetLayer == ProjectLayer.Infrastructure);
    }

    [Fact]
    public void Analyze_WithEntityFile_ClassifiesAsDomain()
    {
        CreateFile("UserEntity.cs", "public class UserEntity {}");
        var result = _sut.Analyze(_tempDir);
        result.Files.Should().ContainSingle(f => f.FileName == "UserEntity.cs" && f.TargetLayer == ProjectLayer.Domain);
    }

    [Fact]
    public void Analyze_WithDbContextFile_ClassifiesAsInfrastructure()
    {
        CreateFile("AppDbContext.cs", "public class AppDbContext {}");
        var result = _sut.Analyze(_tempDir);
        result.Files.Should().ContainSingle(f => f.FileName == "AppDbContext.cs" && f.TargetLayer == ProjectLayer.Infrastructure);
    }

    [Fact]
    public void Analyze_WithHandlerFile_ClassifiesAsApplication_WithHighConfidence()
    {
        CreateFile("CreateOrderHandler.cs", "public class CreateOrderHandler {}");
        var result = _sut.Analyze(_tempDir);
        result.Files.Should().ContainSingle(f => f.FileName == "CreateOrderHandler.cs" && 
            f.TargetLayer == ProjectLayer.Application && f.Confidence == ClassificationConfidence.High);
    }

    [Fact]
    public void Analyze_WithTestFile_ClassifiesAsTests()
    {
        CreateFile("OrderTests.cs", "public class OrderTests {}");
        var result = _sut.Analyze(_tempDir);
        result.Files.Should().ContainSingle(f => f.FileName == "OrderTests.cs" && f.TargetLayer == ProjectLayer.Tests);
    }

    [Fact]
    public void Analyze_WithExistingSln_SetsHasSolutionTrue()
    {
        CreateFile("App.sln");
        var result = _sut.Analyze(_tempDir);
        result.HasSolution.Should().BeTrue();
    }

    [Fact]
    public void Analyze_WithExistingCsproj_PopulatesExistingProjects()
    {
        CreateFile("App.Api/App.Api.csproj");
        var result = _sut.Analyze(_tempDir);
        result.ExistingProjects.Should().HaveCount(1);
    }

    [Fact]
    public void Analyze_WithBinFolder_SkipsBinFolder()
    {
        CreateFile("bin/Debug/net8.0/TestService.cs");
        var result = _sut.Analyze(_tempDir);
        result.Files.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_InfersProjectNameFromCsproj()
    {
        CreateFile("MyAwesomeApp.csproj");
        var result = _sut.Analyze(_tempDir);
        result.ProjectName.Should().Be("MyAwesomeApp");
    }

    [Fact]
    public void Analyze_InfersProjectNameFromDirectoryWhenNoCsproj()
    {
        var result = _sut.Analyze(_tempDir);
        result.ProjectName.Should().Be(new DirectoryInfo(_tempDir).Name);
    }
}
