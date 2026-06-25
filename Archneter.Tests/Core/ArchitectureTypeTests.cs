using Xunit;
using Archneter.Core.Enums;
using FluentAssertions;

namespace Archneter.Tests.Core;

public class ArchitectureTypeTests
{
    [Fact]
    public void Enum_HasExactly5Values()
    {
        var values = Enum.GetValues<ArchitectureType>();
        values.Should().HaveCount(5);
    }

    [Fact]
    public void Enum_ContainsCleanArchitecture()
    {
        Enum.IsDefined(typeof(ArchitectureType), ArchitectureType.CleanArchitecture).Should().BeTrue();
    }

    [Fact]
    public void Enum_ContainsVerticalSlice()
    {
        Enum.IsDefined(typeof(ArchitectureType), ArchitectureType.VerticalSlice).Should().BeTrue();
    }

    [Fact]
    public void Enum_ContainsModularMonolith()
    {
        Enum.IsDefined(typeof(ArchitectureType), ArchitectureType.ModularMonolith).Should().BeTrue();
    }

    [Fact]
    public void Enum_ContainsMicroservices()
    {
        Enum.IsDefined(typeof(ArchitectureType), ArchitectureType.Microservices).Should().BeTrue();
    }

    [Fact]
    public void Enum_ContainsNTier()
    {
        Enum.IsDefined(typeof(ArchitectureType), ArchitectureType.NTier).Should().BeTrue();
    }
}
