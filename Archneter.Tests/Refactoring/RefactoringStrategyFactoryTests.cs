using Xunit;
using Moq;
using Archneter.Core.Enums;
using Archneter.Generators.Refactoring;
using Archneter.Generators.Refactoring.Strategies;
using FluentAssertions;
using Archneter.Generators.Infrastructure;

namespace Archneter.Tests.Refactoring;

public class RefactoringStrategyFactoryTests
{
    private readonly RefactoringStrategyFactory _sut = new(new DotnetCliService(), new DryRunCliService());

    [Fact]
    public void Create_WithCleanArchitecture_ReturnsToCleanArchitectureStrategy()
    {
        var result = _sut.Create(ArchitectureType.CleanArchitecture, false);
        result.Should().BeOfType<ToCleanArchitectureStrategy>();
    }

    [Fact]
    public void Create_WithNTier_ReturnsToNTierStrategy()
    {
        var result = _sut.Create(ArchitectureType.NTier, false);
        result.Should().BeOfType<ToNTierStrategy>();
    }

    [Fact]
    public void Create_WithVerticalSlice_ReturnsToVerticalSliceStrategy()
    {
        var result = _sut.Create(ArchitectureType.VerticalSlice, false);
        result.Should().BeOfType<ToVerticalSliceStrategy>();
    }

    [Fact]
    public void Create_WithModularMonolith_ReturnsToModularMonolithStrategy()
    {
        var result = _sut.Create(ArchitectureType.ModularMonolith, false);
        result.Should().BeOfType<ToModularMonolithStrategy>();
    }

    [Fact]
    public void Create_WithMicroservices_ReturnsToMicroservicesStrategy()
    {
        var result = _sut.Create(ArchitectureType.Microservices, false);
        result.Should().BeOfType<ToMicroservicesStrategy>();
    }

    [Fact]
    public void Create_WithUnsupportedType_ThrowsNotSupportedException()
    {
        var action = () => _sut.Create((ArchitectureType)999, false);
        action.Should().Throw<NotSupportedException>();
    }
}
