using Xunit;
using Archneter.Cli.Commands;
using Archneter.Cli.Models;
using Archneter.Cli.Services;
using Archneter.Core.Abstractions;
using Archneter.Core.Enums;
using Archneter.Core.Models;
using FluentAssertions;
using Moq;

namespace Archneter.Tests.Commands;

public class NewCommandTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<GeneratorFactory> _factoryMock;
    private readonly Mock<IArchitectureGenerator> _generatorMock;
    private readonly NewCommand _sut;

    public NewCommandTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _factoryMock = new Mock<GeneratorFactory>(_serviceProviderMock.Object);
        _generatorMock = new Mock<IArchitectureGenerator>();

        // Setup default Create behavior
        _factoryMock
            .Setup(x => x.Create(It.IsAny<ArchitectureType>(), It.IsAny<bool>()))
            .Returns(_generatorMock.Object);

        _sut = new NewCommand(_factoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCleanArchName_CallsGeneratorOnce()
    {
        // Arrange
        var context = new CommandContext
        {
            Command = "new",
            ProjectName = "TestProject",
            Options = new Dictionary<string, string> { { "--arch", "clean" } }
        };

        // Act
        await _sut.ExecuteAsync(context);

        // Assert
        _generatorMock.Verify(x => x.GenerateAsync(It.Is<ProjectOptions>(o =>
            o.ProjectName == "TestProject" &&
            o.Architecture == ArchitectureType.CleanArchitecture)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingProjectName_PrintsErrorAndReturns()
    {
        // Arrange
        var context = new CommandContext { ProjectName = string.Empty };
        
        var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        await _sut.ExecuteAsync(context);

        // Assert
        var output = sw.ToString();
        output.Should().Contain("Error: project name is required");
        _factoryMock.Verify(x => x.Create(It.IsAny<ArchitectureType>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithMicroservicesAndServices_PassesServiceNames()
    {
        // Arrange
        var context = new CommandContext
        {
            ProjectName = "MicroApp",
            Options = new Dictionary<string, string>
            {
                { "--arch", "microservices" },
                { "--services", "Order,Product" }
            }
        };

        // Act
        await _sut.ExecuteAsync(context);

        // Assert
        _generatorMock.Verify(x => x.GenerateAsync(It.Is<ProjectOptions>(o =>
            o.Architecture == ArchitectureType.Microservices &&
            o.ServiceNames.Contains("Order") &&
            o.ServiceNames.Contains("Product") &&
            o.ServiceNames.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithDryRunFlag_PassesDryRunToFactory()
    {
        // Arrange
        var context = new CommandContext
        {
            ProjectName = "DryRunApp",
            Flags = new HashSet<string> { "--dry-run" }
        };

        // Act
        await _sut.ExecuteAsync(context);

        // Assert
        _factoryMock.Verify(x => x.Create(ArchitectureType.CleanArchitecture, true), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownArch_DefaultsToCleanArchitecture()
    {
        // Arrange
        var context = new CommandContext
        {
            ProjectName = "TestApp",
            Options = new Dictionary<string, string> { { "--arch", "unknown-arch" } }
        };

        // Act
        await _sut.ExecuteAsync(context);

        // Assert
        _generatorMock.Verify(x => x.GenerateAsync(It.Is<ProjectOptions>(o =>
            o.Architecture == ArchitectureType.CleanArchitecture)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithTestsTrue_SetsGenerateTestsTrue()
    {
        // Arrange
        var context = new CommandContext
        {
            ProjectName = "TestApp",
            Options = new Dictionary<string, string> { { "--tests", "true" } }
        };

        // Act
        await _sut.ExecuteAsync(context);

        // Assert
        _generatorMock.Verify(x => x.GenerateAsync(It.Is<ProjectOptions>(o =>
            o.GenerateTests == true)), Times.Once);
    }
}
